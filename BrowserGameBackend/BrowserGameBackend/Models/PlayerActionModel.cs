namespace BrowserGameBackend.Models
{
    /// <summary>
    /// Stores information about player's action or movement
    /// </summary>
    public struct PlayerActionModel
    {
        /// <summary>
        /// direction of player's movement:
        /// 0 - upper-left, 1 - up, 2 - upper-right
        /// 3 - left, 4 - no movement, 5 - right
        /// 4 - bottom-left, 5 - down, 6 - bottom-right
        /// </summary>
        public int MovementDirection;

        /// <summary>
        /// action other than moving or rotating taken by player
        /// 0 - shoot
        /// </summary>
        public int? Action;

        /// <summary>
        /// Player's rotation in radians
        /// </summary>
        public double R;

        /// <summary>
        /// Time at which movement has been recorded unix time milliseconds
        /// </summary>
        public long Timestamp;

        public PlayerActionModel(long timeStamp, int movementDirection, double r, int? action)
        {
            MovementDirection = movementDirection;
            R = r;
            Action = action;
            Timestamp = timeStamp;
        }
    }
}
