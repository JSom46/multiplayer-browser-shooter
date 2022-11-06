namespace BrowserGameBackend.Models
{
    public class GameModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string MapName { get; set; }
        public List<PlayerModel> Players { get; set; } = new List<PlayerModel>();
        public List<ProjectileModel> Projectiles { get; set; } = new List<ProjectileModel>();
        public int MaxPlayers { get; set; }
        public int NextProjectileId { get; set; } = 0;
    }
}
