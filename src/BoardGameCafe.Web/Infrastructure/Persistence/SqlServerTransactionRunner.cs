using System.Data;
using BoardGameCafe.Web.Application.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BoardGameCafe.Web.Infrastructure.Persistence;

public sealed class SqlServerTransactionRunner
{
    private const int AppLockTimeoutMs = 5000;
    private readonly AppDbContext _dbContext;
    
    public SqlServerTransactionRunner(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<Result<T>> ExecuteAsync<T>(
        Guid cafeScopeId,
        DateTime reservationDate,
        Func<CancellationToken, Task<Result<T>>> action,
        CancellationToken ct)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        
        try
        {
            if (!await TryAcquireLockAsync(cafeScopeId, reservationDate, ct))
            {
                return Result<T>.Failure(
                    ErrorCodes.ReservationConflict,
                    "Another booking request is currently being processed for this time slot. Please try again.");
            }
            
            var result = await action(ct);
            if (result.IsSuccess)
            {
                await transaction.CommitAsync(ct);
            }
            
            return result;
        }
        catch (Exception ex) when (IsConcurrencyOrDeadlockException(ex))
        {
            return Result<T>.Failure(ErrorCodes.ReservationConflict, "The selected table or game copy was just booked by another request.");
        }
    }
    
    private async Task<bool> TryAcquireLockAsync(Guid cafeScopeId, DateTime reservationDate, CancellationToken ct)
    {
        if (!_dbContext.Database.IsSqlServer())
        {
            return true;
        }
        
        var lockResource = $"Reservation_Lock_{cafeScopeId:N}_{reservationDate:yyyyMMdd}";
        var returnCode = new SqlParameter("@ReturnCode", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
        
        await _dbContext.Database.ExecuteSqlRawAsync(
            "EXEC @ReturnCode = sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = @LockTimeout",
            [
                returnCode,
                new SqlParameter("@Resource", lockResource),
                new SqlParameter("@LockTimeout", AppLockTimeoutMs)
            ],
            ct);
        
        return returnCode.Value is >= 0;
    }
    
    private static bool IsConcurrencyOrDeadlockException(Exception ex)
    {
        if (ex is DbUpdateConcurrencyException)
        {
            return true;
        }
        
        var sqlException = ex as SqlException ?? ex.InnerException as SqlException;
        return sqlException?.Number is 1205 or 1222 or 2601 or 2627;
    }
}