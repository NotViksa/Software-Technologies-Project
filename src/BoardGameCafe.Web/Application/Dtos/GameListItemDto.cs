namespace BoardGameCafe.Web.Application.Dtos;

public record GameListItemDto(
    Guid Id,
    string Title,
    string Category,
    int MinPlayers,
    int MaxPlayers,
    int DurationMinutes,
    string Complexity,
    int AvailableCopies);
