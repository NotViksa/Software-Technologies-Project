namespace BoardGameCafe.Web.Domain.Entities;

public class ReservationGame
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;
    public Guid? GameCopyId { get; set; }
    public GameCopy? GameCopy { get; set; }
    public bool IsCopyGuaranteed { get; set; }
}
