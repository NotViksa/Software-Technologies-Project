namespace BoardGameCafe.Web.Domain.Entities;

public class Cafe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "Europe/Sofia";
    public TimeSpan OpeningTime { get; set; } = new(10, 0, 0);
    public TimeSpan ClosingTime { get; set; } = new(23, 0, 0);
    public bool IsActive { get; set; } = true;
}
