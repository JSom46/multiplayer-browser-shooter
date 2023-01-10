using System.Text.Json;
using BrowserGame.Models;

namespace BrowserGame.Utils;

public class MapLoader : IMapLoader
{
    public Dictionary<string, MapModel> LoadMaps(string path)
    {
        var maps = new Dictionary<string, MapModel>();

        foreach (var file in Directory.EnumerateFiles(path, "*.json"))
        {
            using var r = new StreamReader(file);
            try
            {
                var json = r.ReadToEnd();
                var map = JsonSerializer.Deserialize<JsonMapModel>(json);

                if (map == null) throw new Exception("Deserialization failure.");

                var obstacles = Array.Find(map.layers, e => e.name == "Obstacles").data;
                var terrain = Array.Find(map.layers, e => e.name == "Terrain").data;

                if (terrain == null || obstacles == null) throw new Exception("Invalid map.");

                var canBeShotThrogh = new bool[map.height][];
                var isTraversable = new bool[map.height][];

                for (var i = 0; i < map.height; i++)
                {
                    canBeShotThrogh[i] = new bool[map.width];
                    isTraversable[i] = new bool[map.width];
                    for (var j = 0; j < map.width; j++)
                    {
                        canBeShotThrogh[i][j] = obstacles[j + i * map.width] - 1 < 0
                            ? true
                            : !map.tilesets[0].tiles[obstacles[j + i * map.width] - 1].properties[0].value;
                        isTraversable[i][j] = canBeShotThrogh[i][j] && (terrain[j + i * map.width] - 1 < 0
                            ? false
                            : !map.tilesets[0].tiles[terrain[j + i * map.width] - 1].properties[0].value);
                    }
                }

                if (!maps.TryAdd(
                        file.Substring(file.LastIndexOf('\\') + 1, file.Length - file.LastIndexOf('.') - 1),
                        new MapModel
                        {
                            Name = file.Substring(file.LastIndexOf('\\') + 1,
                                file.Length - file.LastIndexOf('.') - 1),
                            Width = map.width,
                            Height = map.height,
                            TileHeight = map.tileheight,
                            TileWidth = map.tilewidth,
                            IsTraversable = isTraversable,
                            CanBeShotThrough = canBeShotThrogh
                        }))
                    throw new Exception("Map with this name already exists.");

                Console.WriteLine($"Successfully loaded map from file {file}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Loading map from file {file}  failed: {e.Message}");
            }
        }

        return maps;
    }
}
