namespace BoardGameCafe.Web.Domain.Entities;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int DurationMinutes { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Complexity { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<GameCopy> Copies { get; set; } = new List<GameCopy>();
}
