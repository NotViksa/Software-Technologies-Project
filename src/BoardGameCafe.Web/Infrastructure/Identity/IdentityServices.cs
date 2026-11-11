using BoardGameCafe.Web.Domain.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BoardGameCafe.Web.Infrastructure.Identity;

public static class IdentityServices
{
    public static IServiceCollection AddCafeIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<Persistence.AppDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Identity/Account/Login";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        var authentication = services.AddAuthentication();

        var google = configuration.GetSection("Authentication:Google");
        if (!string.IsNullOrWhiteSpace(google["ClientId"]) && !string.IsNullOrWhiteSpace(google["ClientSecret"]))
        {
            authentication.AddGoogle(options =>
            {
                options.ClientId = google["ClientId"]!;
                options.ClientSecret = google["ClientSecret"]!;
            });
        }

        var facebook = configuration.GetSection("Authentication:Facebook");
        if (!string.IsNullOrWhiteSpace(facebook["AppId"]) && !string.IsNullOrWhiteSpace(facebook["AppSecret"]))
        {
            authentication.AddFacebook(options =>
            {
                options.AppId = facebook["AppId"]!;
                options.AppSecret = facebook["AppSecret"]!;
            });
        }

        return services;
    }
}
