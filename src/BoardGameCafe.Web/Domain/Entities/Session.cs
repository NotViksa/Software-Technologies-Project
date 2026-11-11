namespace BoardGameCafe.Web.Domain.Entities;

public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
