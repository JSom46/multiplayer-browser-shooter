using BrowserGame.Data;
using BrowserGame.Dtos;
using BrowserGame.Models;
using Microsoft.AspNetCore.Mvc;

namespace BrowserGame.Controllers
{
    [ApiController]
    [Route("game")]
    public class GameController : ControllerBase
    {
        private readonly IGameData _games;

        public GameController(IGameData games)
        {
            _games = games;
        }

        [Route("list")]
        [HttpGet]
        public ActionResult<IEnumerable<GameListElement>> GetList()
        {
            return Ok(_games.GetAll().Select(e => new GameListElement()
            {
                Id = e.Id,
                Name = e.Name,
                MapName = e.MapName,
                MaxPlayers = e.MaxPlayers,
                PlayersCount = e.Players.Count
            }));
        }

    }
}