namespace BrowserGameBackend.Dtos
{
    public class ClientStateData
    {  
        /// <summary>
        /// direction of player's movement:
        /// 0 - upper-left, 1 - up, 2 - upper-right
        /// 3 - left, 4 - no movement, 5 - right
        /// 4 - bottom-left, 5 - down, 6 - bottom-right
        /// </summary>
        public int? Dir { get; set; }
        /// <summary>
        /// action other than moving or rotating taken by player
        /// 0 - shoot
        /// </summary>
        public int? Action { get; set; }
        /// <summary>
        /// absolute value of rotation in radians
        /// </summary>
        public double? R { get; set; }
    }
}
