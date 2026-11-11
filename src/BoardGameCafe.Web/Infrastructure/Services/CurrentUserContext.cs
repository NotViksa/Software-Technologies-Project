using System.Security.Claims;
using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Application.Common;

namespace BoardGameCafe.Web.Infrastructure.Services;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsManager => User?.IsInRole(Roles.Manager) ?? false;

    public bool IsStaff => IsManager || (User?.IsInRole(Roles.Employee) ?? false);

    public string? DisplayName => User?.Identity?.Name;
}
