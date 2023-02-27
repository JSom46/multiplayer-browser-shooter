namespace BrowserGame.Models;

/// <summary>
///     Stores deserialized data from .json file that contains map made in Tiled.
/// </summary>
public class JsonMapModel
{
    public int compressionlevel { get; set; }
    public int height { get; set; }
    public bool infinite { get; set; }
    public Layer[] layers { get; set; }
    public int nextlayerid { get; set; }
    public int nextobjectid { get; set; }
    public string orientation { get; set; }
    public string renderorder { get; set; }
    public string tiledversion { get; set; }
    public int tileheight { get; set; }
    public Tileset[] tilesets { get; set; }
    public int tilewidth { get; set; }
    public string type { get; set; }
    public string version { get; set; }
    public int width { get; set; }
}

public class Layer
{
    public int[] data { get; set; }
    public int height { get; set; }
    public int id { get; set; }
    public string name { get; set; }
    public int opacity { get; set; }
    public string type { get; set; }
    public bool visible { get; set; }
    public int width { get; set; }
    public int x { get; set; }
    public int y { get; set; }
}

public class Tileset
{
    public int columns { get; set; }
    public int firstgid { get; set; }
    public string image { get; set; }
    public int imageheight { get; set; }
    public int imagewidth { get; set; }
    public int margin { get; set; }
    public string name { get; set; }
    public int spacing { get; set; }
    public int tilecount { get; set; }
    public int tileheight { get; set; }
    public Tile[] tiles { get; set; }
    public int tilewidth { get; set; }
}

public class Tile
{
    public int id { get; set; }
    public Property1[] properties { get; set; }
}

public class Property1
{
    public string name { get; set; }
    public string type { get; set; }
    public bool value { get; set; }
}