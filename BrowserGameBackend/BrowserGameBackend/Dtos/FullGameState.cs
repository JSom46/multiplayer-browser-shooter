using BrowserGameBackend.Models;

namespace BrowserGameBackend.Dtos
{
    public class FullGameState
    {
        public IEnumerable<FullPlayerState> Players { get; set; }
        public IEnumerable<ProjectileModel> Projectiles { get; set; }
        public long TimeStamp { get; set; }
    }
}
