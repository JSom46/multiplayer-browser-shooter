namespace BrowserGame.Models
{
    public class MapModel
    {
        public string Name { get; set; }

        /// <summary>
        /// Width of map in tiles
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of map in tiles
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Width of tile in points
        /// </summary>
        public int TileWidth { get; set; }

        /// <summary>
        /// Height of tile in points
        /// </summary>
        public int TileHeight { get; set; }

        /// <summary>
        /// Contains information about which tile player can (true) or can't (false) walk on
        /// </summary>
        public bool[][] IsTraversable { get; set; }

        /// <summary>
        /// Contains information about which tile projectile can (true) or can't (false) pass through
        /// </summary>
        public bool[][] CanBeShotThrough { get; set; }
    }
}
