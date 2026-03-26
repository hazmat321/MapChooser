using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;

namespace MapChooser.Commands;

public class MapListCommand
{
    private readonly ISwiftlyCore _core;
    private readonly MapLister _mapLister;
    private readonly MapCooldown _mapCooldown;

    public MapListCommand(ISwiftlyCore core, MapLister mapLister, MapCooldown mapCooldown)
    {
        _core = core;
        _mapLister = mapLister;
        _mapCooldown = mapCooldown;
    }

    public void Execute(ICommandContext context)
    {
        if (!context.IsSentByPlayer) return;
        var player = context.Sender!;
        var localizer = _core.Translation.GetPlayerLocalizer(player);

        var maps = _mapLister.Maps;
        if (maps.Count == 0)
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.maplist.empty"]);
            return;
        }

        player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.maplist.check_console"]);

        player.SendConsole(localizer["map_chooser.maplist.header", maps.Count]);
        for (int i = 0; i < maps.Count; i++)
        {
            var map = maps[i];
            bool inCooldown = _mapCooldown.IsMapInCooldown(map);
            string status = inCooldown ? localizer["map_chooser.maplist.status_cooldown"] : localizer["map_chooser.maplist.status_available"];
            player.SendConsole(localizer["map_chooser.maplist.entry", i + 1, map.Name, status]);
        }
        player.SendConsole(localizer["map_chooser.maplist.footer"]);
    }
}
