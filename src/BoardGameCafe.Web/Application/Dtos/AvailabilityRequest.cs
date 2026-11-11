namespace BoardGameCafe.Web.Application.Dtos;

public class AvailabilityRequest
{
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int PartySize { get; set; }
    public Guid GameId { get; set; }
}
