using BoardGameCafe.Web.Domain.Enums;
using BoardGameCafe.Web.Domain.Rules;

namespace BoardGameCafe.Tests;

public class ReservationStateMachineTests
{
    [Theory]
    [InlineData(ReservationStatus.Pending, ReservationStatus.Confirmed)]
    [InlineData(ReservationStatus.Pending, ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Confirmed, ReservationStatus.CheckedIn)]
    [InlineData(ReservationStatus.Confirmed, ReservationStatus.NoShow)]
    [InlineData(ReservationStatus.CheckedIn, ReservationStatus.InSession)]
    [InlineData(ReservationStatus.InSession, ReservationStatus.Completed)]
    [InlineData(ReservationStatus.InSession, ReservationStatus.Overdue)]
    [InlineData(ReservationStatus.Overdue, ReservationStatus.Completed)]
    public void CanTransition_AllowsValidTransitions(ReservationStatus from, ReservationStatus to)
    {
        Assert.True(ReservationStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(ReservationStatus.Pending, ReservationStatus.CheckedIn)]
    [InlineData(ReservationStatus.Completed, ReservationStatus.Confirmed)]
    [InlineData(ReservationStatus.Cancelled, ReservationStatus.InSession)]
    [InlineData(ReservationStatus.NoShow, ReservationStatus.Confirmed)]
    public void CanTransition_RejectsInvalidTransitions(ReservationStatus from, ReservationStatus to)
    {
        Assert.False(ReservationStateMachine.CanTransition(from, to));
    }

    [Fact]
    public void NextStates_ReturnsExpectedStates()
    {
        var nextStates = ReservationStateMachine.NextStates(ReservationStatus.Confirmed);

        Assert.Contains(ReservationStatus.CheckedIn, nextStates);
        Assert.Contains(ReservationStatus.Cancelled, nextStates);
        Assert.Contains(ReservationStatus.NoShow, nextStates);
        Assert.DoesNotContain(ReservationStatus.Completed, nextStates);
    }
}
