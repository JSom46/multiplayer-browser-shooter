using BrowserGame.Models;

namespace BrowserGame.Utils
{
    public class PlayerPositioner
    {
        private const double Epsilon = 0.00001;

        public static void MovePlayer(PlayerModel player, MapModel map, long delta, double playerHitboxRadius)
        {
            var newX = player.X;
            var newY = player.Y;

            try
            {
                switch (player.LastMovementDirection)
                {
                    // upper - left
                    case 0:
                        {
                            newX += -player.MovementSpeed * delta;
                            newY += -player.MovementSpeed * delta;

                            // player out of map's boundries
                            if (newX - playerHitboxRadius <= 0)
                            {
                                newX = playerHitboxRadius;
                            }
                            if (newY - playerHitboxRadius <= 0)
                            {
                                newY = playerHitboxRadius;
                            }
                            if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                            {
                                newX = map.Width * map.TileWidth - playerHitboxRadius;
                            }
                            if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                            {
                                newY = map.Height * map.TileHeight - playerHitboxRadius;
                            }

                            var y1 = (int)((player.Y - playerHitboxRadius + Epsilon) / map.TileHeight);
                            var y2 = (int)((player.Y + playerHitboxRadius - Epsilon) / map.TileHeight);
                            var x = (int)((newX - playerHitboxRadius) / map.TileWidth);

                            // player can't move left
                            if (y1 < 0 || y2 >= map.Height || x < 0 || !map.IsTraversable[y1][x] || !map.IsTraversable[y2][x])
                            {
                                newX = Math.Floor(player.X / map.TileWidth) * map.TileWidth + playerHitboxRadius;
                            }

                            var y = (int)((newY - playerHitboxRadius) / map.TileHeight);
                            var x1 = (int)((newX - playerHitboxRadius + Epsilon) / map.TileWidth);
                            var x2 = (int)((newX + playerHitboxRadius - Epsilon) / map.TileWidth);

                            // player can't move up
                            if (y < 0 || x1 < 0 || x2 >= map.Width || !map.IsTraversable[y][x1] || !map.IsTraversable[y][x2])
                            {
                                newY = Math.Floor(player.Y / map.TileHeight) * map.TileHeight + playerHitboxRadius;
                            }

                            break;
                        }
                    // up
                    case 1:
                        {
                            newY += -player.MovementSpeed * delta;

                            // player out of map's boundries
                            if (newX - playerHitboxRadius <= 0)
                            {
                                newX = playerHitboxRadius;
                            }
                            if (newY - playerHitboxRadius <= 0)
                            {
                                newY = playerHitboxRadius;
                            }
                            if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                            {
                                newX = map.Width * map.TileWidth - playerHitboxRadius;
                            }
                            if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                            {
                                newY = map.Height * map.TileHeight - playerHitboxRadius;
                            }

                            var y = (int)((newY - playerHitboxRadius) / map.TileHeight);
                            var x1 = (int)((newX - playerHitboxRadius + Epsilon) / map.TileWidth);
                            var x2 = (int)((newX + playerHitboxRadius - Epsilon) / map.TileWidth);

                            // player can't move up
                            if (y < 0 || x1 < 0 || x2 >= map.Width || !map.IsTraversable[y][x1] || !map.IsTraversable[y][x2])
                            {
                                newY = Math.Floor(player.Y / map.TileHeight) * map.TileHeight + playerHitboxRadius;
                            }

                            break;
                        }
                    // upper - right
                    case 2:
                        {
                            newX += player.MovementSpeed * delta;
                            newY += -player.MovementSpeed * delta;

                            // player out of map's boundries
                            if (newX - playerHitboxRadius <= 0)
                            {
                                newX = playerHitboxRadius;
                            }
                            if (newY - playerHitboxRadius <= 0)
                            {
                                newY = playerHitboxRadius;
                            }
                            if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                            {
                                newX = map.Width * map.TileWidth - playerHitboxRadius;
                            }
                            if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                            {
                                newY = map.Height * map.TileHeight - playerHitboxRadius;
                            }

                            var y1 = (int)((player.Y - playerHitboxRadius + Epsilon) / map.TileHeight);
                            var y2 = (int)((player.Y + playerHitboxRadius - Epsilon) / map.TileHeight);
                            var x = (int)((newX + playerHitboxRadius) / map.TileWidth);

                            // player can't move right
                            if (x >= map.Width || y1 < 0 || y2 >= map.Height || !map.IsTraversable[y1][x] || !map.IsTraversable[y2][x])
                            {
                                newX = Math.Ceiling(player.X / map.TileWidth) * map.TileWidth - playerHitboxRadius;
                            }

                            var y = (int)((newY - playerHitboxRadius) / map.TileHeight);
                            var x1 = (int)((newX - playerHitboxRadius + Epsilon) / map.TileWidth);
                            var x2 = (int)((newX + playerHitboxRadius - Epsilon) / map.TileWidth);

                            // player can't move up
                            if (y < 0 || x1 < 0 || x2 >= map.Width || !map.IsTraversable[y][x1] || !map.IsTraversable[y][x2])
                            {
                                newY = Math.Floor(player.Y / map.TileHeight) * map.TileHeight + playerHitboxRadius;
                            }

                            break;
                        }
                    // left
                    case 3:
                        {
                            newX += -player.MovementSpeed * delta;

                            // player out of map's boundries
                            if (newX - playerHitboxRadius <= 0)
                            {
                                newX = playerHitboxRadius;
                            }
                            if (newY - playerHitboxRadius <= 0)
                            {
                                newY = playerHitboxRadius;
                            }
                            if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                            {
                                newX = map.Width * map.TileWidth - playerHitboxRadius;
                            }
                            if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                            {
                                newY = map.Height * map.TileHeight - playerHitboxRadius;
                            }

                            var y1 = (int)((player.Y - playerHitboxRadius + Epsilon) / map.TileHeight);
                            var y2 = (int)((player.Y + playerHitboxRadius - Epsilon) / map.TileHeight);
                            var x = (int)((newX - playerHitboxRadius) / map.TileWidth);

                            // player can't move left
                            if (y1 < 0 || y2 >= map.Height || x <= 0 || !map.IsTraversable[y1][x] || !map.IsTraversable[y2][x])
                            {
                                newX = Math.Floor(player.X / map.TileWidth) * map.TileWidth + playerHitboxRadius;
                            }

                            break;
                        }
                    // right
                    case 5:
                        {
                            newX += player.MovementSpeed * delta;

                            // player out of map's boundries
                            if (newX - playerHitboxRadius <= 0)
                            {
                                newX = playerHitboxRadius;
                            }
                            if (newY - playerHitboxRadius <= 0)
                            {
                                newY = playerHitboxRadius;
                            }
                            if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                            {
                                newX = map.Width * map.TileWidth - playerHitboxRadius;
                            }
                            if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                            {
                                newY = map.Height * map.TileHeight - playerHitboxRadius;
                            }

                            var y1 = (int)((player.Y - playerHitboxRadius + Epsilon) / map.TileHeight);
                            var y2 = (int)((player.Y + playerHitboxRadius - Epsilon) / map.TileHeight);
                            var x = (int)((newX + playerHitboxRadius) / map.TileWidth);

                            // player can't move right
                            if (x >= map.Width || y1 < 0 || y2 >= map.Height || !map.IsTraversable[y1][x] || !map.IsTraversable[y2][x])
                            {
                                newX = Math.Ceiling(player.X / map.TileWidth) * map.TileWidth - playerHitboxRadius;
                            }

                            break;
                        }
                    // bottom - left
                    case 6:
                        {
                            newX += -player.MovementSpeed * delta;
                            newY += player.MovementSpeed * delta;

                            // player out of map's boundries
                            if (newX - playerHitboxRadius <= 0)
                            {
                                newX = playerHitboxRadius;
                            }
                            if (newY - playerHitboxRadius <= 0)
                            {
                                newY = playerHitboxRadius;
                            }
                            if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                            {
                                newX = map.Width * map.TileWidth - playerHitboxRadius;
                            }
                            if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                            {
                                newY = map.Height * map.TileHeight - playerHitboxRadius;
                            }


                            var y1 = (int)((player.Y - playerHitboxRadius + Epsilon) / map.TileHeight);
                            var y2 = (int)((player.Y + playerHitboxRadius - Epsilon) / map.TileHeight);
                            var x = (int)((newX - playerHitboxRadius) / map.TileWidth);

                            // player can't move left
                            if (y1 < 0 || y2 >= map.Height || x < 0 || !map.IsTraversable[y1][x] || !map.IsTraversable[y2][x])
                            {
                                newX = Math.Floor(player.X / map.TileWidth) * map.TileWidth + playerHitboxRadius;
                            }

                            var y = (int)((newY + playerHitboxRadius) / map.TileHeight);
                            var x1 = (int)((newX - playerHitboxRadius + Epsilon) / map.TileWidth);
                            var x2 = (int)((newX + playerHitboxRadius - Epsilon) / map.TileWidth);

                            // player can't move down
                            if (x1 < 0 || x2 >= map.Width || y >= map.Height || !map.IsTraversable[y][x1] || !map.IsTraversable[y][x2])
                            {
                                newY = Math.Ceiling(player.Y / map.TileHeight) * map.TileHeight - playerHitboxRadius;
                            }

                            break;
                        }
                    // bottom
                    case 7:
                        {
                            newY += player.MovementSpeed * delta;

                            // player out of map's boundries
                            if (newX - playerHitboxRadius <= 0)
                            {
                                newX = playerHitboxRadius;
                            }
                            if (newY - playerHitboxRadius <= 0)
                            {
                                newY = playerHitboxRadius;
                            }
                            if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                            {
                                newX = map.Width * map.TileWidth - playerHitboxRadius;
                            }
                            if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                            {
                                newY = map.Height * map.TileHeight - playerHitboxRadius;
                            }

                            var y = (int)((newY + playerHitboxRadius) / map.TileHeight);
                            var x1 = (int)((newX - playerHitboxRadius + Epsilon) / map.TileWidth);
                            var x2 = (int)((newX + playerHitboxRadius - Epsilon) / map.TileWidth);

                            // player can't move down
                            if (x1 < 0 || x2 >= map.Width || y >= map.Height || !map.IsTraversable[y][x1] || !map.IsTraversable[y][x2])
                            {
                                newY = Math.Ceiling(player.Y / map.TileHeight) * map.TileHeight - playerHitboxRadius;
                            }

                            break;
                        }
                    // bottom - right
                    case 8:
                        {
                            newX += player.MovementSpeed * delta;
                            newY += player.MovementSpeed * delta;

                            // player out of map's boundries
                            if (newX - playerHitboxRadius <= 0)
                            {
                                newX = playerHitboxRadius;
                            }
                            if (newY - playerHitboxRadius <= 0)
                            {
                                newY = playerHitboxRadius;
                            }
                            if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                            {
                                newX = map.Width * map.TileWidth - playerHitboxRadius;
                            }
                            if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                            {
                                newY = map.Height * map.TileHeight - playerHitboxRadius;
                            }

                            var y1 = (int)((player.Y - playerHitboxRadius + Epsilon) / map.TileHeight);
                            var y2 = (int)((player.Y + playerHitboxRadius - Epsilon) / map.TileHeight);
                            var x = (int)((newX + playerHitboxRadius) / map.TileWidth);

                            // player can't move right
                            if (x >= map.Width || y1 < 0 || y2 >= map.Height || !map.IsTraversable[y1][x] || !map.IsTraversable[y2][x])
                            {
                                newX = Math.Ceiling(player.X / map.TileWidth) * map.TileWidth - playerHitboxRadius;
                            }

                            var y = (int)((newY + playerHitboxRadius) / map.TileHeight);
                            var x1 = (int)((newX - playerHitboxRadius + Epsilon) / map.TileWidth);
                            var x2 = (int)((newX + playerHitboxRadius - Epsilon) / map.TileWidth);

                            // player can't move down
                            if (x1 < 0 || x2 >= map.Width || y >= map.Height || !map.IsTraversable[y][x1] || !map.IsTraversable[y][x2])
                            {
                                newY = Math.Ceiling(player.Y / map.TileHeight) * map.TileHeight - playerHitboxRadius;
                            }

                            break;
                        }
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine("x: " + player.X + " y: " + player.Y + " direction: " + player.LastMovementDirection + " newx: " +
                                  newX + " newy: " + newY + " y1: " + ((int)(player.Y - playerHitboxRadius) / map.TileHeight) +
                                  " y2: " + (int)((player.Y + playerHitboxRadius) / map.TileHeight) + " x1: " +
                                  (int)((newX - playerHitboxRadius) / map.TileWidth) + " x2: " +
                                  (int)((newX + playerHitboxRadius) / map.TileWidth) + " x + e: " +
                                  (newX + playerHitboxRadius) + " x - e: " + (newX - playerHitboxRadius) + " y + e: " +
                                  (newY + playerHitboxRadius) + " y - e: " + (newY - playerHitboxRadius) + "mapwidth: " +
                                  map.IsTraversable[0].Length + "mapheight" + map.IsTraversable.Length);
                Console.WriteLine(e.StackTrace);
                throw e;
            }

            player.X = newX;
            player.Y = newY;
        }

        /// <summary>
        /// change player's position to a center of random, traversable field
        /// </summary>
        /// <param name="player">player whose position is to be changed</param>
        /// <param name="map">map the player is moving on</param>
        public static void RandomPlayerPosition(PlayerModel player, MapModel map)
        {
            var rng = new Random();
            var x = rng.Next(0, map.Width);
            var y = rng.Next(0, map.Height);

            while (!map.IsTraversable[y][x])
            {
                x = rng.Next(0, map.Width);
                y = rng.Next(0, map.Height);
            }

            player.X = x * map.TileWidth + map.TileWidth * 0.5;
            player.Y = y * map.TileHeight + map.TileHeight * 0.5;
        }
    }
}
