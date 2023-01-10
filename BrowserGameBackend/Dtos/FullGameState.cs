using BrowserGame.Models;

namespace BrowserGame.Dtos;

public class FullGameState
{
    public IEnumerable<FullPlayerState> Players { get; set; }
    public IEnumerable<ProjectileModel> Projectiles { get; set; }
    public long TimeStamp { get; set; }
}