using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Domain.Entities;
using BoardGameCafe.Web.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BoardGameCafe.Web.Infrastructure.Persistence.Seed;

public static class DbInitializer
{
    private const string DemoPassword = "Passw0rd!";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<AppDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database is not ready. Attempt {Attempt}/{MaxAttempts}.", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        await SeedRolesAsync(provider);
        await SeedUsersAsync(provider);
        await SeedCafeAsync(db);
        await SeedZonesAndTablesAsync(db);
        await SeedGamesAsync(db);
    }

    private static async Task SeedRolesAsync(IServiceProvider provider)
    {
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }

    private static async Task SeedUsersAsync(IServiceProvider provider)
    {
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureUserAsync(userManager, "manager@boardgamecafe.local", "Manager", Roles.Manager);
        await EnsureUserAsync(userManager, "employee@boardgamecafe.local", "Employee", Roles.Employee);
        await EnsureUserAsync(userManager, "customer@boardgamecafe.local", "Customer", Roles.Customer);
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName
            };

            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Could not create user '{email}': {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task SeedCafeAsync(AppDbContext db)
    {
        if (await db.Cafes.AnyAsync())
        {
            return;
        }

        db.Cafes.Add(new Cafe
        {
            Name = "Meeple House",
            Address = "бул. Някъде 123, София",
            Description = "Кафене за настолни игри с богат каталог, уютни зони и помощ от гейм мастър.",
            TimeZoneId = "Europe/Sofia",
            OpeningTime = new TimeSpan(10, 0, 0),
            ClosingTime = new TimeSpan(23, 0, 0)
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedZonesAndTablesAsync(AppDbContext db)
    {
        if (await db.Zones.AnyAsync())
        {
            return;
        }

        var mainHall = new Zone
        {
            Name = "Основна зала",
            Description = "Светла зала за компании до 6 души."
        };
        mainHall.Tables.Add(new Table { Name = "Маса 1", Capacity = 2 });
        mainHall.Tables.Add(new Table { Name = "Маса 2", Capacity = 4 });
        mainHall.Tables.Add(new Table { Name = "Маса 3", Capacity = 4 });
        mainHall.Tables.Add(new Table { Name = "Маса 4", Capacity = 6 });

        var quietRoom = new Zone
        {
            Name = "Тиха стая",
            Description = "По-спокойна зона за стратегически игри."
        };
        quietRoom.Tables.Add(new Table { Name = "Маса 5", Capacity = 2 });
        quietRoom.Tables.Add(new Table { Name = "Маса 6", Capacity = 4 });

        db.Zones.AddRange(mainHall, quietRoom);
        await db.SaveChangesAsync();
    }

    private static async Task SeedGamesAsync(AppDbContext db)
    {
        if (await db.Games.AnyAsync())
        {
            return;
        }

        var games = new List<Game>
        {
            new() { Title = "Catan", Description = "Търговия, ресурси и развитие на селища върху остров Катан.", MinPlayers = 3, MaxPlayers = 4, DurationMinutes = 90, Category = "Стратегия", Complexity = "Средна" },
            new() { Title = "Ticket to Ride", Description = "Събирайте вагони и свързвайте градове по железопътна карта.", MinPlayers = 2, MaxPlayers = 5, DurationMinutes = 60, Category = "Семейна", Complexity = "Лека" },
            new() { Title = "Codenames", Description = "Отборна игра с думи, подсказки и внимателно уцелване на агенти.", MinPlayers = 4, MaxPlayers = 8, DurationMinutes = 30, Category = "Парти", Complexity = "Лека" },
            new() { Title = "Dixit", Description = "Асоциативна игра с красиви илюстрации и разказвач на всеки рунд.", MinPlayers = 3, MaxPlayers = 6, DurationMinutes = 30, Category = "Парти", Complexity = "Лека" },
            new() { Title = "Carcassonne", Description = "Поставяйте плочки, строете градове и пътища, събирайте точки.", MinPlayers = 2, MaxPlayers = 5, DurationMinutes = 45, Category = "Стратегия", Complexity = "Лека" },
            new() { Title = "Azul", Description = "Абстрактна игра за подреждане на плочки по кралски модел.", MinPlayers = 2, MaxPlayers = 4, DurationMinutes = 45, Category = "Абстрактна", Complexity = "Средна" },
            new() { Title = "Pandemic", Description = "Кооперативна игра, в която екипът спира глобални епидемии.", MinPlayers = 2, MaxPlayers = 4, DurationMinutes = 60, Category = "Кооперативна", Complexity = "Средна" },
            new() { Title = "Terraforming Mars", Description = "Дълга стратегическа игра за колонизиране и тераформиране на Марс.", MinPlayers = 1, MaxPlayers = 5, DurationMinutes = 120, Category = "Стратегия", Complexity = "Тежка" },
            new() { Title = "7 Wonders", Description = "Развивайте цивилизация и събирайте ресурси за три епохи.", MinPlayers = 3, MaxPlayers = 7, DurationMinutes = 45, Category = "Стратегия", Complexity = "Средна" },
            new() { Title = "Splendor", Description = "Събирайте скъпоценни камъни и развивайте търговска гилдия.", MinPlayers = 2, MaxPlayers = 4, DurationMinutes = 30, Category = "Семейна", Complexity = "Лека" }
        };

        for (var gameIndex = 0; gameIndex < games.Count; gameIndex++)
        {
            var game = games[gameIndex];
            for (var copyNumber = 1; copyNumber <= 2; copyNumber++)
            {
                game.Copies.Add(new GameCopy
                {
                    InventoryCode = $"BG-{gameIndex + 1:000}-{copyNumber:00}",
                    Condition = copyNumber == 1 ? GameCondition.Good : GameCondition.New,
                    Status = GameCopyStatus.Available
                });
            }
        }

        db.Games.AddRange(games);
        await db.SaveChangesAsync();
    }

}
