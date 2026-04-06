using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using System;

namespace MapChooser.Commands;

public class TimeleftCommand
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly MapChooserConfig _config;

    public TimeleftCommand(ISwiftlyCore core, PluginState state, MapChooserConfig config)
    {
        _core = core;
        _state = state;
        _config = config;
    }

    public void Execute(ICommandContext context)
    {
        if (!context.IsSentByPlayer) return;
        var player = context.Sender!;
        var localizer = _core.Translation.GetPlayerLocalizer(player);

        if (_state.WarmupRunning)
        {
            player.SendChat(localizer["map_chooser.timeleft.prefix"] + " " + localizer["map_chooser.general.validation.warmup"]);
            return;
        }

        var timelimitConVar = _core.ConVar.Find<float>("mp_timelimit");
        var maxroundsConVar = _core.ConVar.Find<int>("mp_maxrounds");
        
        float timelimit = timelimitConVar?.Value ?? 0;
        int maxrounds = maxroundsConVar?.Value ?? 0;

        string text;

        if (timelimit > 0)
        {
            float timePlayed = _core.Engine.GlobalVars.CurrentTime - _state.MapStartTime;
            float timeRemaining = (timelimit * 60) - timePlayed;

            if (timeRemaining > 1)
            {
                TimeSpan remaining = TimeSpan.FromSeconds(timeRemaining);
                if (remaining.Hours > 0)
                {
                    text = localizer["map_chooser.timeleft.remaining_time_hour", remaining.Hours.ToString("00"), remaining.Minutes.ToString("00"), remaining.Seconds.ToString("00")];
                }
                else if (remaining.Minutes > 0)
                {
                    text = localizer["map_chooser.timeleft.remaining_time_minute", remaining.Minutes, remaining.Seconds];
                }
                else
                {
                    text = localizer["map_chooser.timeleft.remaining_time_second", remaining.Seconds];
                }
            }
            else
            {
                text = localizer["map_chooser.timeleft.time_over"];
            }
        }
        else if (maxrounds > 0)
        {
            int totalRoundsPlayed = _core.Game.MatchData.TerroristScoreTotal + _core.Game.MatchData.CTScoreTotal;
            int roundsRemaining = maxrounds - totalRoundsPlayed;
            if (roundsRemaining > 1)
            {
                text = localizer["map_chooser.timeleft.remaining_rounds", roundsRemaining];
            }
            else
            {
                text = localizer["map_chooser.timeleft.last_round"];
            }
        }
        else
        {
            text = localizer["map_chooser.timeleft.no_time_limit"];
        }

        player.SendChat(localizer["map_chooser.timeleft.prefix"] + " " + text);
    }
}
