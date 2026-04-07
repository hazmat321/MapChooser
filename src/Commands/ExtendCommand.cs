using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;

namespace MapChooser.Commands;

public class ExtendCommand
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly VoteManager _voteManager;
    private readonly ExtendManager _extendManager;
    private readonly MapChooserConfig _config;

    public ExtendCommand(ISwiftlyCore core, PluginState state, VoteManager voteManager, ExtendManager extendManager, MapChooserConfig config)
    {
        _core = core;
        _state = state;
        _voteManager = voteManager;
        _extendManager = extendManager;
        _config = config;
    }

    public void Execute(ICommandContext context)
    {
        if (!_config.ExtendMap.Enabled) return;
        if (!context.IsSentByPlayer) return;

        var player = context.Sender!;
        var localizer = _core.Translation.GetPlayerLocalizer(player);

        if (_state.WarmupRunning && !_config.ExtendMap.EnabledInWarmup)
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.warmup"]);
            return;
        }

        if (_config.ExtendMap.MinPlayers > 0)
        {
            int playerCount = _core.PlayerManager.GetAllPlayers().Count(p => p.IsValid && !p.IsFakeClient);
            if (playerCount < _config.ExtendMap.MinPlayers)
            {
                player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.min_players", _config.ExtendMap.MinPlayers]);
                return;
            }
        }

        if (_config.ExtendMap.MinRounds > 0)
        {
            int totalRoundsPlayed = _core.Game.MatchData.TerroristScoreTotal + _core.Game.MatchData.CTScoreTotal;
            if (totalRoundsPlayed < _config.ExtendMap.MinRounds)
            {
                player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.min_rounds", _config.ExtendMap.MinRounds - totalRoundsPlayed]);
                return;
            }
        }

        if (_state.ExtendsLeft <= 0)
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.extend.no_extends_left"]);
            return;
        }

        if (!_config.AllowSpectatorsToVote && player.Controller?.TeamNum == 1)
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.spectator"]);
            return;
        }

        if (_voteManager.AddVote(player.Slot))
        {
            var allPlayers = _core.PlayerManager.GetAllPlayers()
                .Where(p => p.IsValid && !p.IsFakeClient && (_config.AllowSpectatorsToVote || p.Controller?.TeamNum > 1))
                .ToList();
            int totalPlayers = allPlayers.Count;
            int needed = _voteManager.GetRequiredVotes(totalPlayers, _config.ExtendMap.VotePercentage);
            
            _core.PlayerManager.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.extend.voted", player.Controller?.PlayerName ?? "Unknown", _voteManager.VoteCount, needed]);

            if (_voteManager.HasReached(totalPlayers, _config.ExtendMap.VotePercentage))
            {
                _voteManager.Clear();
                _extendManager.ExtendMap(_config.EndOfMap.ExtendTimeStep, _config.EndOfMap.ExtendRoundStep);
            }
        }
        else
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.extend.already_voted"]);
        }
    }
}
