namespace BoardGameCafe.Web.Domain.Entities;

public class Table
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ZoneId { get; set; }
    public Zone Zone { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
