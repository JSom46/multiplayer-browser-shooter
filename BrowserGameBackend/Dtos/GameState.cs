using BrowserGame.Models;

namespace BrowserGame.Dtos
{
    public class PlayerState
    {
        public string Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double R { get; set; }
    }
    public class GameState
    {
        public IEnumerable<PlayerState> Players { get; set; }
        public IEnumerable<ProjectileModel> NewProjectiles { get; set; }
        public long TimeStamp { get; set; }
    }
}
