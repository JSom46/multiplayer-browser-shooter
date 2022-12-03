namespace BrowserGameBackend.Dtos
{
    public class GameListElement
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string MapName { get; set; }
        public int PlayersCount { get; set; }
        public int MaxPlayers { get; set; }
    }
}
