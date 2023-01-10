using System.Text.Json;
using BrowserGame.Models;
using BrowserGame.Utils;

namespace BrowserGame.Data;

public class MapData : IMapData
{
    private readonly Dictionary<string, MapModel> _maps = new ();

    public MapData(IMapLoader mapLoader)
    {
        _maps = mapLoader.LoadMaps($"{Directory.GetCurrentDirectory()}\\wwwroot\\assets\\maps");
    }

    public MapModel? GetByName(string name)
    {
        _maps.TryGetValue(name, out var map);
        return map;
    }

    public IEnumerable<MapModel> GetAll()
    {
        return _maps.Values.ToList();
    }
}