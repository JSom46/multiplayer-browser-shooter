using BrowserGame.Models;

namespace BrowserGame.Utils;

public interface IMapLoader
{
    Dictionary<string, MapModel> LoadMaps(string path);
}
