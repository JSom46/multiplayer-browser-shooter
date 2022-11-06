namespace BrowserGameBackend.Models
{
    public class ProjectileModel
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        /// <summary>
        /// Projectile's speed along x axis in points per millisecond
        /// </summary>
        public double SX { get; set; }

        /// <summary>
        /// Projectile's speed along y axis in points per millisecond
        /// </summary>
        public double SY { get; set; }

        /// <summary>
        /// Id of player that fired the projectile
        /// </summary>
        public string PId { get; set; }
    }
}
