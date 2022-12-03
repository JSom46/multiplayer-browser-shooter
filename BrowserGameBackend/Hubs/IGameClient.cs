using BrowserGame.Dtos;

namespace BrowserGame.Hubs
{
    public interface IGameClient
    {
        Task ServerTick(GameState data);
        Task PlayerJoined(string id);
        Task PlayerLeft(string id);
        Task PlayerKilled(string killerId, string killedId);
    }
}
