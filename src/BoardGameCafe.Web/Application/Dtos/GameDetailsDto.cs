namespace BoardGameCafe.Web.Application.Dtos;

public record GameDetailsDto(
    Guid Id,
    string Title,
    string Description,
    string Category,
    int MinPlayers,
    int MaxPlayers,
    int DurationMinutes,
    string Complexity,
    int TotalCopies,
    int AvailableCopies);
