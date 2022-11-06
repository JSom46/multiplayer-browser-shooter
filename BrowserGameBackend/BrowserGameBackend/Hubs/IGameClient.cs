using BrowserGameBackend.Dtos;

namespace BrowserGameBackend.Hubs
{
    public interface IGameClient
    {
        Task ServerTick(GameState data);
        Task PlayerJoined(string id);
        Task PlayerLeft(string id);
        Task PlayerKilled(string killerId, string killedId);
    }
}
