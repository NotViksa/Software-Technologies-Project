using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Domain.Entities;

public class GameCopy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;
    public string InventoryCode { get; set; } = string.Empty;
    public GameCondition Condition { get; set; } = GameCondition.New;
    public GameCopyStatus Status { get; set; } = GameCopyStatus.Available;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<ReservationGame> Reservations { get; set; } = new List<ReservationGame>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
