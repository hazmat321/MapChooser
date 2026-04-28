using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;

namespace MapChooser.Commands;

public class RtvCommand
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly VoteManager _voteManager;
    private readonly EndOfMapVoteManager _eofManager;
    private readonly MapChooserConfig _config;

    public RtvCommand(ISwiftlyCore core, PluginState state, VoteManager voteManager, EndOfMapVoteManager eofManager, MapChooserConfig config)
    {
        _core = core;
        _state = state;
        _voteManager = voteManager;
        _eofManager = eofManager;
        _config = config;
    }

    public void Execute(ICommandContext context)
    {
        if (!_config.Rtv.Enabled) return;
        if (!context.IsSentByPlayer) return;

        var player = context.Sender!;
        var localizer = _core.Translation.GetPlayerLocalizer(player);

        if (_state.WarmupRunning && !_config.Rtv.EnabledInWarmup)
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.warmup"]);
            return;
        }

        if (_config.Rtv.MinPlayers > 0)
        {
            int playerCount = _core.PlayerManager.GetAllPlayers().Count(p => p.IsValid && !p.IsFakeClient);
            if (playerCount < _config.Rtv.MinPlayers)
            {
                player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.min_players", _config.Rtv.MinPlayers]);
                return;
            }
        }

        if (_config.Rtv.MinRounds > 0)
        {
            int totalRoundsPlayed = _core.Game.MatchData.TerroristScoreTotal + _core.Game.MatchData.CTScoreTotal;
            if (totalRoundsPlayed < _config.Rtv.MinRounds)
            {
                player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.min_rounds", _config.Rtv.MinRounds - totalRoundsPlayed]);
                return;
            }
        }

        if (!_config.AllowSpectatorsToVote && player.Controller?.TeamNum == 1)
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.spectator"]);
            return;
        }
        if (_state.MapChangeScheduled)
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.rtv.change_scheduled"]);
            return;
        }

        if (_state.EofVoteHappening)
        {
            if (!_eofManager.HasPlayerVoted(player.Slot))
            {
                _eofManager.OpenVoteMenu(player);
            }
            else
            {
                player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.rtv.already_voted"]);
            }
            return;
        }

        if (_state.RtvCooldownEndTime.HasValue)
        {
            var remaining = (_state.RtvCooldownEndTime.Value - DateTime.Now).TotalSeconds;
            if (remaining > 0)
            {
                player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.rtv.cooldown", (int)remaining]);
                return;
            }
            else
            {
                _state.RtvCooldownEndTime = null;
            }
        }

        if (_voteManager.AddVote(player.Slot))
        {
            var allPlayers = _core.PlayerManager.GetAllPlayers()
                .Where(p => p.IsValid && !p.IsFakeClient && (_config.AllowSpectatorsToVote || p.Controller?.TeamNum > 1))
                .ToList();
            int totalPlayers = allPlayers.Count;
            int needed = _voteManager.GetRequiredVotes(totalPlayers, _config.Rtv.VotePercentage);
            
            if (_config.AnnounceVotes)
                _core.PlayerManager.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.rtv.voted", player.Controller?.PlayerName ?? "Unknown", _voteManager.VoteCount, needed]);

            if (_voteManager.HasReached(totalPlayers, _config.Rtv.VotePercentage))
            {
                // _voteManager.Clear(); // Don't clear votes immediately to allow for retraction
                _eofManager.StartVote(_config.Rtv.VoteDuration, _config.Rtv.MapsToShow, _config.Rtv.ChangeMapImmediately, isRtv: true);
            }
        }
        else
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.rtv.already_voted"]);
        }
    }
}
