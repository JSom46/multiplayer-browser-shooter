namespace BrowserGameBackend.Models
{
    public class PlayerModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        /// <summary>
        /// Rotation of player in radians
        /// </summary>
        public double R { get; set; } = 0;

        /// <summary>
        /// direction of player's movement:
        /// 0 - upper-left, 1 - up, 2 - upper-right
        /// 3 - left, 4 - no movement, 5 - right
        /// 4 - bottom-left, 5 - down, 6 - bottom-right
        /// </summary>
        public int LastMovementDirection { get; set; } = 4;
        public Queue<PlayerActionModel> Moves { get; set; } = new();

        /// <summary>
        /// Player's speed in points per millisecond
        /// </summary>
        public double MovementSpeed { get; set; } = 0.1;
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public bool IsAlive { get; set; } = true;

        /// <summary>
        /// Player's projectiles' speed in points per millisecond
        /// </summary>
        public double ProjectilesSpeed { get; set; } = 0.7;
        public long LastStateUpdate { get; set; }
    }
}
