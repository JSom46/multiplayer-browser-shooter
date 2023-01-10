using BrowserGame.Data;
using BrowserGame.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BrowserGame.Controllers;

[ApiController]
[Route("map")]
public class MapController : ControllerBase
{
    private readonly IMapData _maps;

    public MapController(IMapData mapData)
    {
        _maps = mapData;
    }

    /// <returns>List of names of available maps</returns>
    [Route("list")]
    [HttpGet]
    public ActionResult<IEnumerable<MapListElement>> GetAllMaps()
    {
        var res = _maps.GetAll();
        return Ok(res.Select(e => new MapListElement
        {
            Name = e.Name,
            Height = e.Height * e.TileHeight,
            Width = e.Width * e.TileWidth
        }));
    }
}