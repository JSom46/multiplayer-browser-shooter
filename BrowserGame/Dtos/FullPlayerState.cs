namespace BrowserGame.Dtos;

public class FullPlayerState
{
    public string Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double R { get; set; }
    public double MovementSpeed { get; set; }
    public string Name { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public double ProjectilesSpeed { get; set; }
    public bool IsAlive { get; set; }
}