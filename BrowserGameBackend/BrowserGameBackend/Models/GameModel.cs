namespace BrowserGameBackend.Models
{
    public class GameModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string MapName { get; set; }
        public List<PlayerModel> Players { get; set; } = new();
        public List<ProjectileModel> Projectiles { get; set; } = new();
        public int MaxPlayers { get; set; }
        public int NextProjectileId { get; set; } = 0;
    }
}
