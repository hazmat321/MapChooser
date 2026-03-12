using MapChooser.Models;
using MapChooser.Dependencies;
using SwiftlyS2.Shared;

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

        // Extend time
        if (minutes > 0)
        {
            var timelimitConVar = _core.ConVar.Find<float>("mp_timelimit");
            if (timelimitConVar != null && timelimitConVar.Value > 0)
            {
                timelimitConVar.Value += minutes;
                extendedTime = true;
            }
        }

        // Extend rounds
        if (rounds > 0)
        {
            var maxroundsConVar = _core.ConVar.Find<int>("mp_maxrounds");
            if (maxroundsConVar != null)
            {
                // If maxrounds is 0, it might be using default. Let's assume MR12 (24) as starting point if 0
                if (maxroundsConVar.Value == 0) maxroundsConVar.Value = 24;
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

        // Always set cooldowns to prevent immediate re-triggering of the automated vote
        _state.MapChangeScheduled = false;
        _state.EofVoteCompleted = false;
        _state.NextEofVotePossibleRound = _state.RoundsPlayed + 1;
        if (_core.Engine?.GlobalVars != null)
            _state.NextEofVotePossibleTime = _core.Engine.GlobalVars.CurrentTime + 60.0f; // 1 minute

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
