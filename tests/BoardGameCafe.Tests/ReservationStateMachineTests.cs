using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Domain.Enums;
using BoardGameCafe.Web.Domain.Rules;

namespace BoardGameCafe.Tests;

public class ReservationStateMachineTests
{
    [Theory]
    [InlineData(ReservationStatus.Pending, ReservationStatus.Confirmed, false)]
    [InlineData(ReservationStatus.Pending, ReservationStatus.Cancelled, false)]
    [InlineData(ReservationStatus.Confirmed, ReservationStatus.Cancelled, false)]
    [InlineData(ReservationStatus.Confirmed, ReservationStatus.CheckedIn, true)]
    [InlineData(ReservationStatus.Confirmed, ReservationStatus.NoShow, true)]
    [InlineData(ReservationStatus.CheckedIn, ReservationStatus.InSession, true)]
    [InlineData(ReservationStatus.InSession, ReservationStatus.Completed, true)]
    [InlineData(ReservationStatus.InSession, ReservationStatus.Overdue, true)]
    [InlineData(ReservationStatus.Overdue, ReservationStatus.Completed, true)]
    public void ValidateTransition_ValidTransitions_ReturnsSuccess(ReservationStatus current, ReservationStatus next, bool isStaff)
    {
        var result = ReservationStateMachine.ValidateTransition(current, next, isStaff);
        
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorCode);
    }
    
    [Theory]
    [InlineData(ReservationStatus.Confirmed, ReservationStatus.CheckedIn, false)] // Customer cannot check in
    [InlineData(ReservationStatus.Confirmed, ReservationStatus.NoShow, false)]    // Customer cannot mark NoShow
    [InlineData(ReservationStatus.Cancelled, ReservationStatus.Confirmed, true)]  // Cannot revive cancelled
    [InlineData(ReservationStatus.Completed, ReservationStatus.InSession, true)]  // Cannot reopen completed
    [InlineData(ReservationStatus.Pending, ReservationStatus.InSession, true)]    // Cannot skip CheckedIn
    public void ValidateTransition_InvalidTransitions_ReturnsInvalidStateTransitionError(ReservationStatus current, ReservationStatus next, bool isStaff)
    {
        var result = ReservationStateMachine.ValidateTransition(current, next, isStaff);
        
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InvalidStateTransition, result.ErrorCode);
    }
}