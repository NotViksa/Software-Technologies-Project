using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Application.Dtos;

public record ReservationDto(
    Guid Id,
    Guid TableId,
    string TableName,
    DateTime StartAt,
    DateTime EndAt,
    int PartySize,
    ReservationStatus Status,
    string GameTitle,
    string? GameCopyCode);
