using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Domain.Rules;

public static class ReservationStateMachine
{
    public static bool CanTransition(ReservationStatus current, ReservationStatus next, bool isStaff)
    {
        return (current, next) switch
        {
            // Pending -> Confirmed (Customer or Staff) or Cancelled (Customer or Staff)
            (ReservationStatus.Pending, ReservationStatus.Confirmed) => true,
            (ReservationStatus.Pending, ReservationStatus.Cancelled) => true,
            
            // Confirmed -> CheckedIn (Staff), Cancelled (Customer or Staff), NoShow (Staff)
            (ReservationStatus.Confirmed, ReservationStatus.CheckedIn) => isStaff,
            (ReservationStatus.Confirmed, ReservationStatus.Cancelled) => true,
            (ReservationStatus.Confirmed, ReservationStatus.NoShow) => isStaff,
            
            // CheckedIn -> InSession (Staff)
            (ReservationStatus.CheckedIn, ReservationStatus.InSession) => isStaff,
            
            // InSession -> Completed (Staff) or Overdue (Staff / Background check)
            (ReservationStatus.InSession, ReservationStatus.Completed) => isStaff,
            (ReservationStatus.InSession, ReservationStatus.Overdue) => isStaff,
            
            // Overdue -> Completed (Staff)
            (ReservationStatus.Overdue, ReservationStatus.Completed) => isStaff,

            _ => false
        };
    }
    
    public static Result ValidateTransition(ReservationStatus current, ReservationStatus next, bool isStaff)
    {
        if (CanTransition(current, next, isStaff))
        {
            return Result.Success();
        }

        return Result.Failure(ErrorCodes.InvalidStateTransition, 
        $"Invalid reservation state transition from '{current}' to '{next}'.");
    }
}