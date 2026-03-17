using Admins.Menu.Contract;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using MapChooser.Menu;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;

namespace MapChooser.Commands;

public class AdminChangeMapCommand
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly MapLister _mapLister;
    private readonly ChangeMapManager _changeMapManager;

    public AdminChangeMapCommand(ISwiftlyCore core, PluginState state, MapLister mapLister, ChangeMapManager changeMapManager)
    {
        _core = core;
        _state = state;
        _mapLister = mapLister;
        _changeMapManager = changeMapManager;
    }

    public void Execute(ICommandContext context)
    {
        var player = context.Sender!;
        var map = context.Args.Length > 0 ? context.Args[0] : null;
        if (string.IsNullOrEmpty(map))
        {
            if (!context.IsSentByPlayer)
            {
                context.Reply("This command can only be used by players.");
                return;
            }

            var menu = new AdminChangeMapMenu(_core, _mapLister);
            menu.Show(player, HandleChangeMap);
            return;
        }

        var localizer = _core.Translation.GetPlayerLocalizer(player);

        var mapInfo = _mapLister.Maps.FirstOrDefault(m => m.Name.Contains(map, StringComparison.OrdinalIgnoreCase) || (m.Id != null && m.Id.Equals(map, StringComparison.OrdinalIgnoreCase)));
        if (mapInfo == null)
        {
            context.Reply(localizer["map_chooser.change_map.not_found", map]);
            return;
        }

        _state.NextMap = mapInfo.Name;
        _changeMapManager.ChangeMap();
    }

    private void HandleChangeMap(IPlayer player, string mapName)
    {
            var mapInfo = _mapLister.Maps.FirstOrDefault(m => m.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase) || (m.Id != null && m.Id.Equals(mapName, StringComparison.OrdinalIgnoreCase)));
            if (mapInfo == null)
            {
                var localizer = _core.Translation.GetPlayerLocalizer(player);
                player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.map_not_found"]);
                return;
            }
    
            _state.NextMap = mapInfo.Name;
            _changeMapManager.ChangeMap();
    }
}
