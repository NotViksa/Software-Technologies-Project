using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Domain.Entities;

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerUserId { get; set; }
    public ApplicationUser Customer { get; set; } = null!;
    public Guid TableId { get; set; }
    public Table Table { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int PartySize { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<ReservationGame> Games { get; set; } = new List<ReservationGame>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
