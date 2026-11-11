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
        var customer = await SeedUsersAsync(provider);
        await SeedCafeAsync(db);
        await SeedZonesAndTablesAsync(db);
        await SeedGamesAsync(db);
        await SeedReservationsAsync(db, customer);
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

    private static async Task<ApplicationUser> SeedUsersAsync(IServiceProvider provider)
    {
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureUserAsync(userManager, "manager@boardgamecafe.local", "Виктор Петров", Roles.Manager);
        await EnsureUserAsync(userManager, "employee@boardgamecafe.local", "Иван Христов", Roles.Employee);
        return await EnsureUserAsync(userManager, "customer@boardgamecafe.local", "Мария Димитрова", Roles.Customer);
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
            Address = "бул. Витоша 42, София",
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

    private static async Task SeedReservationsAsync(AppDbContext db, ApplicationUser customer)
    {
        if (await db.Reservations.AnyAsync())
        {
            return;
        }

        var smallTable = await db.Tables.OrderBy(x => x.Capacity).FirstOrDefaultAsync();
        var largeTable = await db.Tables.Where(x => x.Capacity >= 4).OrderBy(x => x.Capacity).FirstOrDefaultAsync();
        var games = await db.Games.OrderBy(x => x.Title).Take(2).ToListAsync();

        if (smallTable is null || largeTable is null || games.Count < 2)
        {
            return;
        }

        var today = DateTime.Today;

        var pending = new Reservation
        {
            CustomerUserId = customer.Id,
            TableId = smallTable.Id,
            StartAt = today.AddHours(18),
            EndAt = today.AddHours(20),
            PartySize = 2,
            Status = ReservationStatus.Pending,
            Notes = "Демо резервация"
        };
        pending.Games.Add(new ReservationGame { GameId = games[0].Id });

        var confirmed = new Reservation
        {
            CustomerUserId = customer.Id,
            TableId = largeTable.Id,
            StartAt = today.AddDays(1).AddHours(19),
            EndAt = today.AddDays(1).AddHours(21),
            PartySize = 4,
            Status = ReservationStatus.Confirmed
        };
        confirmed.Games.Add(new ReservationGame { GameId = games[1].Id, IsCopyGuaranteed = false });

        db.Reservations.AddRange(pending, confirmed);
        await db.SaveChangesAsync();
    }
}
