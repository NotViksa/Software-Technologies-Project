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

    public const int GracePeriodMinutes = 15;

    public bool IsOverdue(DateTime currentTimeUtc)
    {
        if (Status != LoanStatus.Active || ReturnedAt != null)
        {
            return false;
        }

        return currentTimeUtc > DueAt.AddMinutes(GracePeriodMinutes);
    }

    public void MarkAsOverdue(DateTime currentTimeUtc)
    {
        if (IsOverdue(currentTimeUtc))
        {
            Status = LoanStatus.Overdue;
        }
    }

    public void CompleteReturn(DateTime returnedAtUtc, bool isDamaged)
    {
        if (ReturnedAt != null)
        {
            throw new InvalidOperationException("Това заемане вече е приключено.");
        }

        ReturnedAt = returnedAtUtc;

        if (isDamaged)
        {
            Status = LoanStatus.Damaged;
        }
        else
        {
            Status = LoanStatus.Returned;
        }
    }
}
    

