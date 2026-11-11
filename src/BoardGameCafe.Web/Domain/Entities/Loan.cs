using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Domain.Entities;

public class Loan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public Guid? SessionId { get; set; }
    public Session? Session { get; set; }
    public Guid GameCopyId { get; set; }
    public GameCopy GameCopy { get; set; } = null!;
    public DateTime LentAt { get; set; } = DateTime.UtcNow;
    public DateTime DueAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
