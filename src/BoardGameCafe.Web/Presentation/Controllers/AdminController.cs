
using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BoardGameCafe.Web.Domain.Entities;

namespace BoardGameCafe.Web.Presentation.Controllers;

[Authorize(Roles = Roles.Manager)]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // ADMIN PANEL

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // CAFE MANAGEMENT

    [HttpGet]
    public async Task<IActionResult> Cafe()
    {
        var cafe = await _db.Cafes
            .FirstOrDefaultAsync(x => x.IsActive);

        if (cafe == null)
            return NotFound();

        return View(cafe);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cafe(
        Guid id,
        string name,
        string address,
        string? description,
        TimeSpan openingTime,
        TimeSpan closingTime)
    {
        var cafe = await _db.Cafes.FindAsync(id);

        if (cafe == null)
            return NotFound();

        if (!ModelState.IsValid ||
            string.IsNullOrWhiteSpace(name) ||
            name.Length > 120 ||
            string.IsNullOrWhiteSpace(address) ||
            address.Length > 200 ||
            (description?.Length ?? 0) > 2000 ||
            openingTime < TimeSpan.Zero ||
            closingTime >= TimeSpan.FromDays(1) ||
            closingTime <= openingTime)
        {
            ModelState.AddModelError(
                "", "Провери въведените данни.");

            return View(cafe);
        }

        var oldName = cafe.Name;
        var oldAddress = cafe.Address;
        var oldOpeningTime = cafe.OpeningTime;
        var oldClosingTime = cafe.ClosingTime;
        var descriptionChanged =
            cafe.Description != (description ?? "");

        cafe.Name = name.Trim();
        cafe.Address = address.Trim();
        cafe.Description = description ?? "";
        cafe.OpeningTime = openingTime;
        cafe.ClosingTime = closingTime;

        await AddAuditLogAsync(
            "Редактиране",
            "Кафене",
            $"Име: {oldName} → {cafe.Name}. " +
            $"Адрес: {oldAddress} → {cafe.Address}. " +
            $"Работно време: " +
            $"{oldOpeningTime:hh\\:mm} - {oldClosingTime:hh\\:mm} → " +
            $"{openingTime:hh\\:mm} - {closingTime:hh\\:mm}. " +
            $"Променено описание: {(descriptionChanged ? "Да" : "Не")}");

        await _db.SaveChangesAsync();

        TempData["Success"] = "Промените са запазени.";

        return RedirectToAction(nameof(Cafe));
    }

    // ZONE MANAGEMENT

    [HttpGet]
    public async Task<IActionResult> Zones()
    {
        var zones = await _db.Zones
            .OrderBy(x => x.Name)
            .ToListAsync();

        return View(zones);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateZone(
        string name,
        string? description)
    {
        if (!ModelState.IsValid ||
            string.IsNullOrWhiteSpace(name) ||
            name.Length > 80 ||
            (description?.Length ?? 0) > 500)
        {
            TempData["Error"] = "Невалидни данни.";
            return RedirectToAction(nameof(Zones));
        }

        name = name.Trim();

        if (await _db.Zones.AnyAsync(
            x => x.Name == name))
        {
            TempData["Error"] = "Зоната вече съществува.";
            return RedirectToAction(nameof(Zones));
        }

        var zone = new Zone
        {
            Name = name,
            Description = description ?? "",
            IsActive = true
        };

        _db.Zones.Add(zone);

        await AddAuditLogAsync(
            "Добавяне",
            "Зона",
            $"Създадена зона: {zone.Name}. " +
            $"Описание: {zone.Description}");

        await _db.SaveChangesAsync();

        TempData["Success"] = "Зоната е добавена.";

        return RedirectToAction(nameof(Zones));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateZone(
        Guid id,
        string name,
        string? description,
        bool isActive)
    {
        var zone = await _db.Zones.FindAsync(id);

        if (zone == null)
            return NotFound();

        if (!ModelState.IsValid ||
            string.IsNullOrWhiteSpace(name) ||
            name.Length > 80 ||
            (description?.Length ?? 0) > 500)
        {
            TempData["Error"] = "Невалидни данни.";
            return RedirectToAction(nameof(Zones));
        }

        name = name.Trim();

        if (await _db.Zones.AnyAsync(
            x => x.Name == name && x.Id != id))
        {
            TempData["Error"] = "Името вече се използва.";
            return RedirectToAction(nameof(Zones));
        }

        var oldName = zone.Name;
        var oldStatus = zone.IsActive;
        var oldDescription = zone.Description;

        zone.Name = name;
        zone.Description = description ?? "";
        zone.IsActive = isActive;

        await AddAuditLogAsync(
            "Редактиране",
            "Зона",
            $"Име: {oldName} → {zone.Name}. " +
            $"Статус: {FormatStatus(oldStatus)} → " +
            $"{FormatStatus(zone.IsActive)}. " +
            $"Описание: {oldDescription} → {zone.Description}");

        await _db.SaveChangesAsync();

        TempData["Success"] = "Зоната е обновена.";

        return RedirectToAction(nameof(Zones));
    }

    // TABLE MANAGEMENT

    [HttpGet]
    public async Task<IActionResult> Tables()
    {
        ViewBag.Zones = await _db.Zones
            .OrderBy(x => x.Name)
            .ToListAsync();

        var tables = await _db.Tables
            .Include(x => x.Zone)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return View(tables);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTable(
        string name,
        int capacity,
        Guid zoneId)
    {
        if (!ModelState.IsValid ||
            string.IsNullOrWhiteSpace(name) ||
            name.Length > 40 ||
            capacity <= 0 ||
            !await _db.Zones.AnyAsync(
                x => x.Id == zoneId && x.IsActive))
        {
            TempData["Error"] = "Невалидни данни.";
            return RedirectToAction(nameof(Tables));
        }

        name = name.Trim();

        if (await _db.Tables.AnyAsync(
            x => x.Name == name &&
                 x.ZoneId == zoneId))
        {
            TempData["Error"] = "Масата вече съществува.";
            return RedirectToAction(nameof(Tables));
        }

        var table = new BoardGameCafe.Web.Domain.Entities.Table
        {
            Name = name,
            Capacity = capacity,
            ZoneId = zoneId,
            IsActive = true
        };

        var zone = await _db.Zones.FindAsync(zoneId);

        _db.Tables.Add(table);

        await AddAuditLogAsync(
            "Добавяне",
            "Маса",
            $"Създадена маса: {table.Name}. " +
            $"Капацитет: {table.Capacity}. " +
            $"Зона: {zone?.Name}");

        await _db.SaveChangesAsync();

        TempData["Success"] = "Масата е добавена.";

        return RedirectToAction(nameof(Tables));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTable(
        Guid id,
        string name,
        int capacity,
        Guid zoneId,
        bool isActive)
    {
        var table = await _db.Tables.FindAsync(id);

        if (table == null)
            return NotFound();

        if (!ModelState.IsValid ||
            string.IsNullOrWhiteSpace(name) ||
            name.Length > 40 ||
            capacity <= 0 ||
            !await _db.Zones.AnyAsync(
                x => x.Id == zoneId &&
                (x.IsActive || !isActive)))
        {
            TempData["Error"] = "Невалидни данни.";
            return RedirectToAction(nameof(Tables));
        }

        name = name.Trim();

        if (await _db.Tables.AnyAsync(
            x => x.Name == name &&
                 x.ZoneId == zoneId &&
                 x.Id != id))
        {
            TempData["Error"] = "Масата вече съществува.";
            return RedirectToAction(nameof(Tables));
        }

        var oldName = table.Name;
        var oldCapacity = table.Capacity;
        var oldStatus = table.IsActive;

        var oldZone = await _db.Zones.FindAsync(table.ZoneId);
        var newZone = await _db.Zones.FindAsync(zoneId);

        table.Name = name;
        table.Capacity = capacity;
        table.ZoneId = zoneId;
        table.IsActive = isActive;

        await AddAuditLogAsync(
            "Редактиране",
            "Маса",
            $"Име: {oldName} → {table.Name}. " +
            $"Капацитет: {oldCapacity} → {table.Capacity}. " +
            $"Зона: {oldZone?.Name} → {newZone?.Name}. " +
            $"Статус: {FormatStatus(oldStatus)} → " +
            $"{FormatStatus(table.IsActive)}");

        await _db.SaveChangesAsync();

        TempData["Success"] = "Масата е обновена.";

        return RedirectToAction(nameof(Tables));
    }

    // USER MANAGEMENT

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users
            .OrderBy(x => x.Email)
            .ToListAsync();

        var userRoles = new Dictionary<Guid, string>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            userRoles[user.Id] = string.Join(", ", roles);
        }

        ViewBag.UserRoles = userRoles;

        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRole(
        Guid id,
        string role)
    {
        if (!ModelState.IsValid ||
            (role != Roles.Customer &&
             role != Roles.Employee &&
             role != Roles.Manager))
        {
            TempData["Error"] = "Невалидна роля.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(
            id.ToString());

        if (user == null)
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);

        if (!Guid.TryParse(currentUserId, out var actorId))
            return Forbid();

        if (actorId == id)
        {
            TempData["Error"] =
                "Не можеш да променяш собствената си роля.";

            return RedirectToAction(nameof(Users));
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var currentRoles =
            await _userManager.GetRolesAsync(user);

        if (currentRoles.Count == 1 &&
            currentRoles.Contains(role))
        {
            TempData["Success"] =
                "Потребителят вече има тази роля.";

            return RedirectToAction(nameof(Users));
        }

        var oldRoles = string.Join(", ", currentRoles);

        var removeResult =
            await _userManager.RemoveFromRolesAsync(
                user, currentRoles);

        if (!removeResult.Succeeded)
        {
            await transaction.RollbackAsync();

            TempData["Error"] =
                "Грешка при промяна на ролята.";

            return RedirectToAction(nameof(Users));
        }

        var addResult =
            await _userManager.AddToRoleAsync(user, role);

        if (!addResult.Succeeded)
        {
            await transaction.RollbackAsync();

            TempData["Error"] = "Ролята не е променена.";

            return RedirectToAction(nameof(Users));
        }

        var stampResult =
            await _userManager.UpdateSecurityStampAsync(user);

        if (!stampResult.Succeeded)
        {
            await transaction.RollbackAsync();

            TempData["Error"] = "Ролята не е променена.";

            return RedirectToAction(nameof(Users));
        }

        await AddAuditLogAsync(
            "Промяна на роля",
            "Потребител",
            $"Потребител: {user.Email}. " +
            $"Стара роля: {oldRoles}. " +
            $"Нова роля: {role}");

        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        TempData["Success"] = "Ролята е променена успешно.";

        return RedirectToAction(nameof(Users));
    }

    // AUDIT LOG

    private async Task AddAuditLogAsync(
        string action,
        string entityName,
        string details)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            throw new InvalidOperationException(
                "Администраторът не е намерен.");

        var log = new AuditLog
        {
            UserId = user.Id,
            UserEmail = user.Email ?? "",
            Action = action,
            EntityName = entityName,
            Details = details.Length > 4000
                ? details.Substring(0, 4000)
                : details,
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(log);
    }

    private static string FormatStatus(bool isActive)
    {
        return isActive ? "Активна" : "Неактивна";
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var logs = await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(500)
            .ToListAsync();

        return View(logs);
    }
}
