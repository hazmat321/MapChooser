using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;

namespace MapChooser.Helpers;

public class ChangeMapManager
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly MapLister _mapLister;
    private readonly MapChooserConfig _config;

    public ChangeMapManager(ISwiftlyCore core, PluginState state, MapLister mapLister, MapChooserConfig config)
    {
        _core = core;
        _state = state;
        _mapLister = mapLister;
        _config = config;
    }

    public void ScheduleMapChange(string mapName, bool changeImmediately = false, bool isRtv = false)
    {
        _state.NextMap = mapName;
        _state.MapChangeScheduled = true;
        _state.ChangeMapImmediately = changeImmediately;
        _state.IsRtv = isRtv;

        if (changeImmediately || _state.MatchEnded)
        {
            ChangeMap();
        }
        else
        {
            _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.next_map_announced", mapName]);
        }
    }

    public void ChangeMap()
    {
        if (string.IsNullOrEmpty(_state.NextMap)) return;

        var mapName = _state.NextMap;
        _state.NextMap = null;
        _state.MapChangeScheduled = false;
        _state.ChangeMapImmediately = true;

        var map = _mapLister.Maps.FirstOrDefault(m => m.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase));
        if (map == null) return;

        int delay = _state.IsRtv ? _config.Rtv.ChangeMapDelay : _config.EndOfMap.ChangeMapDelay;
        _state.IsRtv = false;
        _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.changing_map", map.Name, delay]);
        
        _core.Scheduler.DelayBySeconds(delay, () => {
            if (!string.IsNullOrEmpty(map.Id) && (map.Id.StartsWith("ws:") || long.TryParse(map.Id, out _)))
            {
                string workshopId = map.Id.StartsWith("ws:") ? map.Id.Substring(3) : map.Id;
                _core.Engine.ExecuteCommandWithBuffer($"nextlevel {map.Name}", _ => { });
                _core.Engine.ExecuteCommandWithBuffer($"host_workshop_map {workshopId}", _ => { });
            }
            else
            {
                _core.Engine.ExecuteCommandWithBuffer($"nextlevel {map.Name}", _ => { });
                _core.Engine.ExecuteCommandWithBuffer($"changelevel {map.Id ?? map.Name}", _ => { });
            }
        });
    }
}
