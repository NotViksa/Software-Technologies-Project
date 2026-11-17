using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Application.Dtos;
using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Application.Abstractions;

public interface IInventoryService
{
    // 1. Copy availability: намира първия свободен екземпляр за даден часови интервал (+ 15 мин буфер)
    Task<Guid?> FindAvailableCopyIdAsync(Guid gameId, DateTime startAtUtc, DateTime endAtUtc, CancellationToken ct = default);

    // 2. Списък с всички екземпляри за дадена игра (за служителите)
    Task<IReadOnlyList<GameCopyDto>> GetCopiesByGameAsync(Guid gameId, CancellationToken ct = default);

    // 3. Loans: предаване на играта на клиентите (създава Loan и сменя статуса на InUse)
    Task<Result<LoanDto>> LendCopyAsync(Guid reservationId, Guid gameCopyId, DateTime dueAtUtc, CancellationToken ct = default);

    // 4. Return: връщане на играта и оценка на състоянието ѝ
    Task<Result<LoanDto>> ReturnCopyAsync(Guid loanId, GameCondition conditionAfterReturn, CancellationToken ct = default);

    // 5. Damage: отбелязване на повреда и намиране на засегнатите бъдещи резервации
    Task<Result<DamageReportResultDto>> MarkCopyDamagedAsync(Guid gameCopyId, CancellationToken ct = default);

    // 6. Overdue правила: проверява всички активни заемания и маркира закъснелите (над 15 мин) като Overdue
    Task<int> ProcessOverdueLoansAsync(DateTime currentTimeUtc, CancellationToken ct = default);
}
