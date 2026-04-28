using MapChooser.Commands;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using MapChooser.Models;
using Microsoft.Extensions.Configuration;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace MapChooser;

[PluginMetadata(Id = "MapChooser", Version = "1.1.0", Name = "Map Chooser", Author = "aga", Description = "Map chooser plugin for SwiftlyS2")]
public sealed class MapChooser : BasePlugin {
    private MapChooserConfig _config = new();
    private MapsConfig _mapsConfig = new();
    private PluginState _state = new();
    private MapLister _mapLister = new();
    private MapCooldown _mapCooldown = null!;
    private ChangeMapManager _changeMapManager = null!;
    private VoteManager _rtvVoteManager = null!;
    private VoteManager _extVoteManager = null!;
    private EndOfMapVoteManager _eofManager = null!;
    private ExtendManager _extendManager = null!;
    
    private RtvCommand _rtvCmd = null!;
    private UnRtvCommand _unRtvCmd = null!;
    private NominateCommand _nominateCmd = null!;
    private TimeleftCommand _timeleftCmd = null!;
    private NextmapCommand _nextmapCmd = null!;
    private VotemapCommand _votemapCmd = null!;
    private RevoteCommand _revoteCmd = null!;
    private SetNextMapCommand _setNextMapCmd = null!;
    private ExtendCommand _extendCmd = null!;
    private AdminMapsVoteCommand _adminMapsVoteCmd = null!;
    private AdminChangeMapCommand _adminChangeMapCmd = null!;
    private MapListCommand _mapListCmd = null!;

    public MapChooser(ISwiftlyCore core) : base(core)
    {
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager) {
    }

    public override void Load(bool hotReload) {
        Core.Configuration
            .InitializeJsonWithModel<MapChooserConfig>("config.jsonc", "MapChooser")
            .Configure(builder => {
                builder.AddJsonFile("config.jsonc", optional: false, reloadOnChange: true);
            });

        Core.Configuration
            .InitializeJsonWithModel<MapsConfig>("maps.jsonc", "MapChooserMaps")
            .Configure(builder => {
                builder.AddJsonFile("maps.jsonc", optional: false, reloadOnChange: true);
            });

        _config = Core.Configuration.Manager.GetSection("MapChooser").Get<MapChooserConfig>() ?? new MapChooserConfig();
        _mapsConfig = Core.Configuration.Manager.GetSection("MapChooserMaps").Get<MapsConfig>() ?? new MapsConfig();
        _mapLister.UpdateMaps(_mapsConfig.Maps);
        
        _mapCooldown = new MapCooldown(Core, _config);
        _changeMapManager = new ChangeMapManager(Core, _state, _mapLister, _config);
        _rtvVoteManager = new VoteManager();
        _extVoteManager = new VoteManager();
        _extendManager = new ExtendManager(Core, _state, _config);
        _eofManager = new EndOfMapVoteManager(Core, _state, _rtvVoteManager, _mapLister, _mapCooldown, _changeMapManager, _extendManager, _config);

        _state.ExtendsLeft = _config.EndOfMap.ExtendLimit;
        _state.NextEofVotePossibleRound = 0;
        _state.NextEofVotePossibleTime = 0;
        _state.RoundsPlayed = 0;
        var warmupConVar = Core.ConVar.Find<int>("mp_warmup_period");
        _state.WarmupRunning = warmupConVar?.Value == 1;

        _rtvCmd = new RtvCommand(Core, _state, _rtvVoteManager, _eofManager, _config);
        _unRtvCmd = new UnRtvCommand(Core, _state, _rtvVoteManager, _eofManager, _config);
        _nominateCmd = new NominateCommand(Core, _state, _mapLister, _mapCooldown, _config);
        _timeleftCmd = new TimeleftCommand(Core, _state, _config);
        _nextmapCmd = new NextmapCommand(Core, _state);
        _votemapCmd = new VotemapCommand(Core, _state, _mapLister, _mapCooldown, _changeMapManager, _config);
        _revoteCmd = new RevoteCommand(Core, _state, _eofManager, _config);
        _setNextMapCmd = new SetNextMapCommand(Core, _state, _mapLister, _changeMapManager);
        _extendCmd = new ExtendCommand(Core, _state, _extVoteManager, _extendManager, _config);
        _adminMapsVoteCmd = new AdminMapsVoteCommand(Core, _state, _mapLister, _eofManager, _config);
        _adminChangeMapCmd = new AdminChangeMapCommand(Core, _state, _mapLister, _changeMapManager);
        _mapListCmd = new MapListCommand(Core, _mapLister, _mapCooldown);

        RegisterCommands(_config.Commands.Rtv, _rtvCmd.Execute);
        RegisterCommands(_config.Commands.UnRtv, _unRtvCmd.Execute);
        RegisterCommands(_config.Commands.Nominate, _nominateCmd.Execute);
        RegisterCommands(_config.Commands.Timeleft, _timeleftCmd.Execute);
        RegisterCommands(_config.Commands.Nextmap, _nextmapCmd.Execute);
        RegisterCommands(_config.Commands.Votemap, _votemapCmd.Execute);
        RegisterCommands(_config.Commands.Revote, _revoteCmd.Execute);
        RegisterCommands(_config.Commands.SetNextMap, _setNextMapCmd.Execute, permission: _config.SetNextMapPermission);
        RegisterCommands(_config.Commands.Extend, _extendCmd.Execute);
        RegisterCommands(_config.Commands.MapsVote, _adminMapsVoteCmd.Execute, permission: _config.MapsVotePermission);
        RegisterCommands(_config.Commands.ChangeMap, _adminChangeMapCmd.Execute, permission: _config.ChangeMapPermission);
        RegisterCommands(_config.Commands.MapList, _mapListCmd.Execute);

        Core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        Core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
        Core.GameEvent.HookPost<EventRoundAnnounceWarmup>(OnAnnounceWarmup);
        Core.GameEvent.HookPost<EventWarmupEnd>(OnWarmupEnd);
        Core.GameEvent.HookPost<EventCsWinPanelMatch>(OnWinPanelMatch);
        Core.GameEvent.HookPost<EventGamePhaseChanged>(OnGamePhaseChanged);
        Core.GameEvent.HookPost<EventRoundAnnounceMatchStart>(OnMatchStart);
        Core.GameEvent.HookPost<EventRoundAnnounceMatchPoint>(OnMatchPoint);
        Core.Event.OnMapLoad += OnMapLoad;

        Core.Scheduler.DelayAndRepeat(1000, 1000, () =>
        {
            CheckAutomatedVote();
        });
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        if (string.IsNullOrEmpty(@event.MapName)) return;

        _eofManager?.ResetVote();
        _state.MapChangeScheduled = false;
        _state.EofVoteHappening = false;
        _state.NextMap = null;
        _state.RoundsPlayed = 0;
        try {
            _state.MapStartTime = Core.Engine is { } e ? e.GlobalVars.CurrentTime : 0;
        } catch {
            _state.MapStartTime = 0;
        }
        
        _state.RtvCooldownEndTime = null;
        _state.IsRtv = false;
        _state.ChangeMapImmediately = false;
        
        _rtvVoteManager?.Clear();
        _extVoteManager?.Clear();
        _nominateCmd?.Clear();
        _state.ExtendsLeft = _config.EndOfMap.ExtendLimit;
        _state.NextEofVotePossibleRound = 0;
        _state.NextEofVotePossibleTime = 0;
        _state.MatchEnded = false;
        _state.EofVoteCompleted = false;
        
        _mapCooldown.OnMapStart(@event.MapName, Core.Engine.WorkshopId);
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        CheckAutomatedVote();
        return HookResult.Continue;
    }

    private HookResult OnAnnounceWarmup(EventRoundAnnounceWarmup @event)
    {
        _state.WarmupRunning = true;
        return HookResult.Continue;
    }

    private HookResult OnWarmupEnd(EventWarmupEnd @event)
    {
        _state.WarmupRunning = false;
        try {
            _state.MapStartTime = Core.Engine is { } e ? e.GlobalVars.CurrentTime : 0;
        } catch {
            _state.MapStartTime = 0;
        }
        return HookResult.Continue;
    }

    private HookResult OnMatchStart(EventRoundAnnounceMatchStart @event)
    {
        _eofManager?.ResetVote();
        _state.RoundsPlayed = 0;
        try {
            _state.MapStartTime = Core.Engine is { } e ? e.GlobalVars.CurrentTime : 0;
        } catch {
            _state.MapStartTime = 0;
        }
        _state.WarmupRunning = false;
        _state.NextEofVotePossibleRound = 0;
        _state.NextEofVotePossibleTime = 0;
        _state.MapChangeScheduled = false;
        _state.EofVoteHappening = false;
        _state.EofVoteCompleted = false;
        _state.IsRtv = false;
        _state.ChangeMapImmediately = false;
        _state.NextMap = null;
        _state.ExtendsLeft = _config.EndOfMap.ExtendLimit;
        
        _rtvVoteManager?.Clear();
        _extVoteManager?.Clear();
        return HookResult.Continue;
    }

    private HookResult OnMatchPoint(EventRoundAnnounceMatchPoint @event)
    {
        CheckAutomatedVote(true);
        return HookResult.Continue;
    }

    private HookResult OnWinPanelMatch(EventCsWinPanelMatch @event)
    {
        CCSMatch match;
        try
        {
            match = Core.Game.MatchData;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("GameRules not found", StringComparison.OrdinalIgnoreCase))
        {
            return HookResult.Continue;
        }

        if (match.Phase == GamePhase.GAMEPHASE_HALFTIME) return HookResult.Continue;

        _state.MatchEnded = true;
        if (_state.EofVoteHappening)
            _eofManager.ForceEnd();
        else if (_state.MapChangeScheduled)
            _changeMapManager.ChangeMap();
        return HookResult.Continue;
    }

    private HookResult OnGamePhaseChanged(EventGamePhaseChanged @event)
    {
        if (@event.NewPhase != (short)GamePhase.GAMEPHASE_MATCH_ENDED) return HookResult.Continue;
        if (_state.MatchEnded) return HookResult.Continue;

        _state.MatchEnded = true;
        if (_state.EofVoteHappening)
            _eofManager.ForceEnd();
        else if (_state.MapChangeScheduled)
            _changeMapManager.ChangeMap();
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        _state.RoundsPlayed++;
        if (_state.MapChangeScheduled && !_state.EofVoteHappening && !_state.ChangeMapImmediately && _state.IsRtv)
        {
            _changeMapManager.ChangeMap();
        }
        else if (!_state.MapChangeScheduled)
        {
            CheckAutomatedVote();
        }

        return HookResult.Continue;
    }

    private void CheckAutomatedVote(bool force = false)
    {
        if (!_config.EndOfMap.Enabled || _state.EofVoteHappening || _state.MapChangeScheduled || _state.WarmupRunning) return;

        CCSMatch match;
        try
        {
            match = Core.Game.MatchData;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("GameRules not found", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (match.Phase == GamePhase.GAMEPHASE_HALFTIME) return;

        int totalRoundsPlayed = match.TerroristScoreTotal + match.CTScoreTotal;

        bool pastDueNoMap = _state.EofVoteCompleted && string.IsNullOrEmpty(_state.NextMap);

        if (!force && !pastDueNoMap)
        {
            if (_state.EofVoteCompleted) return;
            if (totalRoundsPlayed < _state.NextEofVotePossibleRound) return;
            if (Core.Engine != null && Core.Engine.GlobalVars.CurrentTime < _state.NextEofVotePossibleTime) return;
        }

        var timelimitConVar = Core.ConVar.Find<float>("mp_timelimit");
        var maxroundsConVar = Core.ConVar.Find<int>("mp_maxrounds");
        var winlimitConVar = Core.ConVar.Find<int>("mp_winlimit");
        
        float timelimit = timelimitConVar?.Value ?? 0;
        int maxrounds = maxroundsConVar?.Value ?? 0;
        int winlimit = winlimitConVar?.Value ?? 0;

        bool trigger = false;

        if (timelimit > 0 && Core.Engine != null)
        {
            if (_state.MapStartTime <= 0)
            {
                _state.MapStartTime = Core.Engine.GlobalVars.CurrentTime;
            }
            float timePlayed = Core.Engine.GlobalVars.CurrentTime - _state.MapStartTime;
            float timeRemaining = (timelimit * 60) - timePlayed;
            if (timeRemaining <= _config.EndOfMap.TriggerSecondsBeforeEnd)
            {
                trigger = true;
            }
        }

        if (!trigger && maxrounds > 0)
        {
            int roundsRemaining = maxrounds - totalRoundsPlayed;
            if (roundsRemaining <= _config.EndOfMap.TriggerRoundsBeforeEnd)
            {
                trigger = true;
            }
        }

        if (!trigger && winlimit > 0)
        {
            var teams = Core.EntitySystem.GetAllEntitiesByClass<CCSTeam>();
            int maxTeamScore = 0;
            foreach (var team in teams)
            {
                int score = team.ScoreFirstHalf + team.ScoreSecondHalf + team.ScoreOvertime;
                if (score > maxTeamScore) maxTeamScore = score;
            }

            if (winlimit - maxTeamScore <= _config.EndOfMap.TriggerRoundsBeforeEnd)
            {
                trigger = true;
            }
        }

        if (trigger)
        {
            _state.EofVoteCompleted = false;
            _state.NextEofVotePossibleRound = totalRoundsPlayed + 1;
            if (Core.Engine != null)
                _state.NextEofVotePossibleTime = Core.Engine.GlobalVars.CurrentTime + _config.EndOfMap.VoteDuration + 1;
            _eofManager.StartVote(_config.EndOfMap.VoteDuration, _config.EndOfMap.MapsToShow);
        }
    }

    private void RegisterCommands(string commandNames, ICommandService.CommandListener handler, string? permission = null)
    {
        var names = commandNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var name in names)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (permission != null)
                    Core.Command.RegisterCommand(name, handler, permission: permission);
                else
                    Core.Command.RegisterCommand(name, handler);
            }
        }
    }

    public override void Unload() {
    }
}
