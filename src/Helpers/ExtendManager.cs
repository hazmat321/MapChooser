using MapChooser.Models;
using MapChooser.Dependencies;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;

namespace MapChooser.Helpers;

public class ExtendManager
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly MapChooserConfig _config;

    public ExtendManager(ISwiftlyCore core, PluginState state, MapChooserConfig config)
    {
        _core = core;
        _state = state;
        _config = config;
    }

    public void ExtendMap(int minutes, int rounds)
    {
        if (_state.ExtendsLeft <= 0) return;

        bool extendedTime = false;
        bool extendedRounds = false;

        if (minutes > 0)
        {
            var timelimitConVar = _core.ConVar.Find<float>("mp_timelimit");
            if (timelimitConVar != null && timelimitConVar.Value > 0)
            {
                timelimitConVar.Value += minutes;
                extendedTime = true;
            }
        }

        if (rounds > 0)
        {
            var maxroundsConVar = _core.ConVar.Find<int>("mp_maxrounds");
            if (maxroundsConVar != null && maxroundsConVar.Value > 0)
            {
                maxroundsConVar.Value += rounds;
                extendedRounds = true;
            }
            
            var winlimitConVar = _core.ConVar.Find<int>("mp_winlimit");
            if (winlimitConVar != null && winlimitConVar.Value > 0)
            {
                winlimitConVar.Value += (int)Math.Ceiling(rounds / 2.0);
                extendedRounds = true;
            }
        }

        _state.MapChangeScheduled = false;
        _state.EofVoteCompleted = false;

        CCSMatch match;
        try
        {
            match = _core.Game.MatchData;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("GameRules not found", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _state.NextEofVotePossibleRound = match.TerroristScoreTotal + match.CTScoreTotal + 1;
        if (_core.Engine?.GlobalVars != null)
            _state.NextEofVotePossibleTime = _core.Engine.GlobalVars.CurrentTime + 60.0f;

        if (extendedTime || extendedRounds)
        {
            _state.ExtendsLeft--;

            if (extendedTime && extendedRounds)
                _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.vote.map_extended_both", minutes, rounds, _state.ExtendsLeft]);
            else if (extendedTime)
                _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.vote.map_extended_time", minutes, _state.ExtendsLeft]);
            else if (extendedRounds)
                _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.vote.map_extended_rounds", rounds, _state.ExtendsLeft]);
        }
    }
}
