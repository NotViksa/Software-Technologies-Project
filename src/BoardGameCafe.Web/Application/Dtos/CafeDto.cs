namespace BoardGameCafe.Web.Application.Dtos;

public record CafeDto(
    string Name,
    string Address,
    string Description,
    string TimeZoneId,
    TimeSpan OpeningTime,
    TimeSpan ClosingTime);
