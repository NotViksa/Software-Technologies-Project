using BoardGameCafe.Web.Application.Dtos;

namespace BoardGameCafe.Web.Presentation.ViewModels;

public class GamesIndexViewModel
{
    public IReadOnlyList<GameListItemDto> Games { get; init; } = [];
    public IReadOnlyList<string> Categories { get; init; } = [];
    public string? Search { get; init; }
    public string? Category { get; init; }
}
