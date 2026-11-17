using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Application.Dtos;

// Инфо за конкретен физически екземпляр
public class GameCopyDto
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public string GameTitle { get; set; } = string.Empty;
    public string InventoryCode { get; set; } = string.Empty;
    public GameCondition Condition { get; set; }
    public GameCopyStatus Status { get; set; }
}

// Инфо за заемане на игра на маса
public class LoanDto
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public Guid GameCopyId { get; set; }
    public string InventoryCode { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public DateTime LentAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public LoanStatus Status { get; set; }
}

// Резултат при отбелязване на повреда - връща списък със засегнатите бъдещи резервации
public class DamageReportResultDto
{
    public Guid DamagedCopyId { get; set; }
    public List<Guid> AffectedReservationIds { get; set; } = new();
}