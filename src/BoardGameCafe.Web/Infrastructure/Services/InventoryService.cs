using BoardGameCafe.Web.Application.Abstractions;
using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Application.Dtos;
using BoardGameCafe.Web.Domain.Entities;
using BoardGameCafe.Web.Domain.Enums;
using BoardGameCafe.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoardGameCafe.Web.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private const int BufferMinutes = 15;
    private readonly AppDbContext _db;

    public InventoryService(AppDbContext db)
    {
        _db = db;
    }

    // 1 търсене на свободен екземпляр за даден часови интервал (+ 15 мин буфер)
    public async Task<Guid?> FindAvailableCopyIdAsync(
        Guid gameId,
        DateTime startAtUtc,
        DateTime endAtUtc,
        CancellationToken ct = default)
    {
        // изтегляне на всички копия от базата в списък
        var allCopies = await _db.GameCopies.ToListAsync(ct);
        var validCopies = new List<GameCopy>();

        // отделяне на само здравите копия на търсената игра
        foreach (var copy in allCopies)
        {
            if (copy.GameId == gameId
                && copy.Status != GameCopyStatus.Damaged
                && copy.Status != GameCopyStatus.Retired
                && copy.Condition != GameCondition.Damaged)
            {
                validCopies.Add(copy);
            }
        }

        if (validCopies.Count == 0)
        {
            return null;
        }

        // подреждане на копията по инвентарен код 
        for (int i = 0; i < validCopies.Count - 1; i++)
        {
            for (int j = i + 1; j < validCopies.Count; j++)
            {
                if (string.Compare(validCopies[i].InventoryCode, validCopies[j].InventoryCode) > 0)
                {
                    var temp = validCopies[i];
                    validCopies[i] = validCopies[j];
                    validCopies[j] = temp;
                }
            }
        }

        // изтегляне на резервациите и връзките им с игри
        var allReservationGames = await _db.ReservationGames.ToListAsync(ct);
        var allReservations = await _db.Reservations.ToListAsync(ct);

        var requestedEndWithBuffer = endAtUtc.AddMinutes(BufferMinutes);

        // проверявана на всяко валидно копие дали е заето в този интервал
        foreach (var copy in validCopies)
        {
            if (copy.Status == GameCopyStatus.InUse && startAtUtc <= DateTime.UtcNow)
            {
                continue;
            }

            bool isBusy = false;

            foreach (var rg in allReservationGames)
            {
                if (rg.GameCopyId != copy.Id)
                {
                    continue;
                }

                // намиране на  самата резервация, за да видим часовете и статуса ѝ
                Reservation? reservation = FindReservationById(allReservations, rg.ReservationId);
                if (reservation == null)
                {
                    continue;
                }

                // пропускане на отказаните и приключените резервации
                if (reservation.Status == ReservationStatus.Cancelled
                    || reservation.Status == ReservationStatus.NoShow
                    || reservation.Status == ReservationStatus.Completed)
                {
                    continue;
                }

                DateTime existingStart = reservation.StartAt;
                DateTime existingEndWithBuffer = reservation.EndAt.AddMinutes(BufferMinutes);

                // проверка дали двата интервала се застъпват
                if (startAtUtc < existingEndWithBuffer && requestedEndWithBuffer > existingStart)
                {
                    isBusy = true;
                    break;
                }
            }

            if (!isBusy)
            {
                return copy.Id;
            }
        }

        return null;
    }

    // 2 връща списък с всички екземпляри на дадена игра
    public async Task<IReadOnlyList<GameCopyDto>> GetCopiesByGameAsync(Guid gameId, CancellationToken ct = default)
    {
        var allGames = await _db.Games.ToListAsync(ct);
        var allCopies = await _db.GameCopies.ToListAsync(ct);

        string gameTitle = string.Empty;
        foreach (var game in allGames)
        {
            if (game.Id == gameId)
            {
                gameTitle = game.Title;
                break;
            }
        }

        var result = new List<GameCopyDto>();

        foreach (var copy in allCopies)
        {
            if (copy.GameId == gameId)
            {
                result.Add(new GameCopyDto
                {
                    Id = copy.Id,
                    GameId = copy.GameId,
                    GameTitle = gameTitle,
                    InventoryCode = copy.InventoryCode,
                    Condition = copy.Condition,
                    Status = copy.Status
                });
            }
        }

        return result;
    }

    // 3 предаване на екземпляр на клиенти
    public async Task<Result<LoanDto>> LendCopyAsync(
        Guid reservationId,
        Guid gameCopyId,
        DateTime dueAtUtc,
        CancellationToken ct = default)
    {
        var copy = await _db.GameCopies.FirstOrDefaultAsync(c => c.Id == gameCopyId, ct);
        if (copy == null)
        {
            return Result<LoanDto>.Failure(ErrorCodes.NotFound, "Екземплярът не е намерен.");
        }

        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId, ct);
        if (reservation == null)
        {
            return Result<LoanDto>.Failure(ErrorCodes.NotFound, "Резервацията не е намерена.");
        }

        try
        {
            copy.MarkAsInUse();
        }
        catch (InvalidOperationException ex)
        {
            return Result<LoanDto>.Failure(ErrorCodes.CopyNotAvailable, ex.Message);
        }

        var loan = new Loan
        {
            ReservationId = reservationId,
            GameCopyId = gameCopyId,
            LentAt = DateTime.UtcNow,
            DueAt = dueAtUtc,
            Status = LoanStatus.Active
        };

        _db.Loans.Add(loan);
        await _db.SaveChangesAsync(ct);

        string gameTitle = await GetGameTitleAsync(copy.GameId, ct);
        return Result<LoanDto>.Success(CreateLoanDto(loan, copy, gameTitle));
    }

    // 4 връщане на екземпляр от клиенти
    public async Task<Result<LoanDto>> ReturnCopyAsync(
        Guid loanId,
        GameCondition conditionAfterReturn,
        CancellationToken ct = default)
    {
        var loan = await _db.Loans.FirstOrDefaultAsync(l => l.Id == loanId, ct);
        if (loan == null)
        {
            return Result<LoanDto>.Failure(ErrorCodes.NotFound, "Записът за заемане не е намерен.");
        }

        var copy = await _db.GameCopies.FirstOrDefaultAsync(c => c.Id == loan.GameCopyId, ct);
        if (copy == null)
        {
            return Result<LoanDto>.Failure(ErrorCodes.NotFound, "Екземплярът не е намерен.");
        }

        bool isDamaged = (conditionAfterReturn == GameCondition.Damaged);

        try
        {
            loan.CompleteReturn(DateTime.UtcNow, isDamaged);
            copy.MarkAsReturned(conditionAfterReturn);
        }
        catch (InvalidOperationException ex)
        {
            return Result<LoanDto>.Failure(ErrorCodes.ValidationError, ex.Message);
        }

        await _db.SaveChangesAsync(ct);

        string gameTitle = await GetGameTitleAsync(copy.GameId, ct);
        return Result<LoanDto>.Success(CreateLoanDto(loan, copy, gameTitle));
    }

    // 5 отбелязване на повреда и пренасочване на бъдещи резервации
    public async Task<Result<DamageReportResultDto>> MarkCopyDamagedAsync(
        Guid gameCopyId,
        CancellationToken ct = default)
    {
        var copy = await _db.GameCopies.FirstOrDefaultAsync(c => c.Id == gameCopyId, ct);
        if (copy == null)
        {
            return Result<DamageReportResultDto>.Failure(ErrorCodes.NotFound, "Екземплярът не е намерен.");
        }

        copy.MarkAsDamaged();

        var allReservationGames = await _db.ReservationGames.ToListAsync(ct);
        var allReservations = await _db.Reservations.ToListAsync(ct);
        var affectedIds = new List<Guid>();
        DateTime now = DateTime.UtcNow;

        foreach (var rg in allReservationGames)
        {
            if (rg.GameCopyId != gameCopyId)
            {
                continue;
            }

            Reservation? reservation = FindReservationById(allReservations, rg.ReservationId);
            if (reservation == null)
            {
                continue;
            }

            // проверяване на само бъдещите активни резервации
            if (reservation.StartAt >= now
                && (reservation.Status == ReservationStatus.Pending || reservation.Status == ReservationStatus.Confirmed))
            {
                Guid? replacementId = await FindAvailableCopyIdAsync(
                    rg.GameId,
                    reservation.StartAt,
                    reservation.EndAt,
                    ct);

                if (replacementId != null && replacementId != gameCopyId)
                {
                    rg.GameCopyId = replacementId;
                }
                else
                {
                    rg.GameCopyId = null;
                    affectedIds.Add(rg.ReservationId);
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        return Result<DamageReportResultDto>.Success(new DamageReportResultDto
        {
            DamagedCopyId = gameCopyId,
            AffectedReservationIds = affectedIds
        });
    }

    // 6 проверка за закъснели заемания (над 15 минути след крайния час)
    public async Task<int> ProcessOverdueLoansAsync(DateTime currentTimeUtc, CancellationToken ct = default)
    {
        var allLoans = await _db.Loans.ToListAsync(ct);
        int updatedCount = 0;

        foreach (var loan in allLoans)
        {
            if (loan.Status == LoanStatus.Active && loan.ReturnedAt == null)
            {
                if (loan.IsOverdue(currentTimeUtc))
                {
                    loan.MarkAsOverdue(currentTimeUtc);
                    updatedCount++;
                }
            }
        }

        if (updatedCount > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        return updatedCount;
    }

    // помощен метод за линейно търсене на резервация в списък
    private static Reservation? FindReservationById(List<Reservation> reservations, Guid reservationId)
    {
        foreach (var r in reservations)
        {
            if (r.Id == reservationId)
            {
                return r;
            }
        }

        return null;
    }

    // помощен метод за намиране на името на играта
    private async Task<string> GetGameTitleAsync(Guid gameId, CancellationToken ct)
    {
        var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == gameId, ct);
        if (game != null)
        {
            return game.Title;
        }

        return string.Empty;
    }

    // помощен метод за създаване на LoanDto
    private static LoanDto CreateLoanDto(Loan loan, GameCopy copy, string gameTitle)
    {
        return new LoanDto
        {
            Id = loan.Id,
            ReservationId = loan.ReservationId,
            GameCopyId = loan.GameCopyId,
            InventoryCode = copy.InventoryCode,
            GameTitle = gameTitle,
            LentAt = loan.LentAt,
            DueAt = loan.DueAt,
            ReturnedAt = loan.ReturnedAt,
            Status = loan.Status
        };
    }
}