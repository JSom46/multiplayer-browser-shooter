using BrowserGameBackend.Models;

namespace BrowserGameBackend.Data
{
    public interface IMapData
    {
        MapModel? GetByName(string name);
        IEnumerable<MapModel> GetAll();
    }
}
