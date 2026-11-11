using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Domain.Rules;

public static class ReservationStateMachine
{
    private static readonly IReadOnlyDictionary<ReservationStatus, ReservationStatus[]> Transitions =
        new Dictionary<ReservationStatus, ReservationStatus[]>
        {
            [ReservationStatus.Pending] = [ReservationStatus.Confirmed, ReservationStatus.Cancelled],
            [ReservationStatus.Confirmed] = [ReservationStatus.CheckedIn, ReservationStatus.Cancelled, ReservationStatus.NoShow],
            [ReservationStatus.CheckedIn] = [ReservationStatus.InSession],
            [ReservationStatus.InSession] = [ReservationStatus.Completed, ReservationStatus.Overdue],
            [ReservationStatus.Overdue] = [ReservationStatus.Completed],
            [ReservationStatus.Completed] = [],
            [ReservationStatus.Cancelled] = [],
            [ReservationStatus.NoShow] = []
        };

    public static bool CanTransition(ReservationStatus from, ReservationStatus to)
    {
        return Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static IReadOnlyCollection<ReservationStatus> NextStates(ReservationStatus from)
    {
        return Transitions.TryGetValue(from, out var allowed) ? allowed : [];
    }
}
