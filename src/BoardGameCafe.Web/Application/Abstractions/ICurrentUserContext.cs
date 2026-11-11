namespace BoardGameCafe.Web.Application.Abstractions;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsManager { get; }
    bool IsStaff { get; }
    string? DisplayName { get; }
}
