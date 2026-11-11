using BoardGameCafe.Web.Application.Common;

namespace BoardGameCafe.Tests;

public class ResultTests
{
    [Fact]
    public void Success_IsSuccessfulAndHasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Failure_CarriesErrorCodeAndMessage()
    {
        var result = Result.Failure(ErrorCodes.ReservationConflict, "Table is already booked.");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ReservationConflict, result.ErrorCode);
        Assert.Equal("Table is already booked.", result.ErrorMessage);
    }

    [Fact]
    public void GenericSuccess_CarriesValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_HasNoValue()
    {
        var result = Result<int>.Failure(ErrorCodes.NotFound, "Missing.");

        Assert.True(result.IsFailure);
        Assert.Equal(0, result.Value);
    }
}
