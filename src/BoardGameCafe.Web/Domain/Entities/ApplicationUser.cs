using Microsoft.AspNetCore.Identity;

namespace BoardGameCafe.Web.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
