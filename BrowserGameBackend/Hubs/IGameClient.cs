using BrowserGame.Dtos;

namespace BrowserGame.Hubs
{
    public interface IGameClient
    {
        Task PlayerJoined(FullPlayerState playerState);
        Task PlayerLeft(string connectionId);
        Task PlayerKilled(string playerId, string? KillerId);
        Task GameJoined(FullGameState gameState);
        Task GameLeft();
        Task CreateError();
        Task JoinError();
        Task LeaveError();
        Task ServerTick(GameState gameState);
    }
}
