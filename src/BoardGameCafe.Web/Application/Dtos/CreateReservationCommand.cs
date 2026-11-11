namespace BoardGameCafe.Web.Application.Dtos;

public class CreateReservationCommand
{
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int PartySize { get; set; }
    public Guid GameId { get; set; }
    public string Notes { get; set; } = string.Empty;
}
