namespace BoardGameCafe.Web.Application.Dtos;

public record DashboardStatsDto(
    int TodayReservations,
    int PendingReservations,
    int ActiveSessions,
    int OverdueLoans,
    int AvailableCopies,
    int DamagedCopies,
    int Games,
    int Tables,
    int Zones);
