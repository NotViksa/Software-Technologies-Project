namespace BoardGameCafe.Web.Application.Common;

public static class ErrorCodes
{
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string ValidationError = "VALIDATION_ERROR";
    
    // Reservation module error codes
    public const string ReservationConflict = "RESERVATION_CONFLICT";
    public const string CopyNotAvailable = "COPY_NOT_AVAILABLE";
    public const string InvalidStateTransition = "INVALID_STATE_TRANSITION";
}
