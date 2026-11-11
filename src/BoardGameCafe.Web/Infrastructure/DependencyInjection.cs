using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Infrastructure.Persistence;
using BoardGameCafe.Web.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BoardGameCafe.Web.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<ICatalogueService, CatalogueService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
