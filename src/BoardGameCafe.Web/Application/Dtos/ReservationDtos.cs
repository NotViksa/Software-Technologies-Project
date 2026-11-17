using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Application.Dtos;

public record AvailabilityRequest(
    DateTime StartAt,
    DateTime EndAt,
    int PartySize,
    Guid? GameId = null,
    Guid? CafeId = null);

public record AvailableTableDto(
    Guid TableId,
    string TableName,
    string ZoneName,
    int Capacity);

public record AvailabilityResult(
    bool IsAvailable,
    Guid? RecommendedTableId,
    Guid? AvailableGameCopyId,
    IReadOnlyList<AvailableTableDto> AvailableTables,
    string? FailureCode = null,
    string? FailureReason = null);

public record CreateReservationCommand(
    Guid CustomerUserId,
    DateTime StartAt,
    DateTime EndAt,
    int PartySize,
    Guid? GameId = null,
    Guid? PreferredTableId = null,
    string? Notes = null,
    Guid? CafeId = null);

public record ReservationDto(
    Guid Id,
    Guid CustomerUserId,
    Guid TableId,
    string TableName,
    DateTime StartAt,
    DateTime EndAt,
    int PartySize,
    ReservationStatus Status,
    Guid? GameId,
    Guid? GameCopyId,
    bool IsCopyGuaranteed);