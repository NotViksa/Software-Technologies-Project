using BoardGameCafe.Web.Application.Dtos;

namespace BoardGameCafe.Web.Presentation.ViewModels;

public class HomeViewModel
{
    public CafeDto? Cafe { get; init; }
    public CatalogueSummaryDto Summary { get; init; } = new(0, 0, 0);
    public IReadOnlyList<GameListItemDto> Games { get; init; } = [];
}
