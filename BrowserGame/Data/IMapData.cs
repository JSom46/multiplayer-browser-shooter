using BrowserGame.Models;

namespace BrowserGame.Data;

public interface IMapData
{
    MapModel? GetByName(string name);
    IEnumerable<MapModel> GetAll();
}