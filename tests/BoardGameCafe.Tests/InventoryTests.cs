using BoardGameCafe.Web.Domain.Entities;
using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Tests;

public class InventoryTests
{
    // Тест 1: повреден екземпляр не може да участва в разпределение за резервации
    [Fact]
    public void CanBeBooked_WhenCopyIsDamaged_ReturnsFalse()
    {
        var copy = new GameCopy
        {
            InventoryCode = "BG-001-01",
            Condition = GameCondition.Damaged,
            Status = GameCopyStatus.Damaged
        };

        bool canBeBooked = copy.CanBeBooked();

        Assert.False(canBeBooked);
    }

    // Тест 2: здрав и свободен екземпляр може да се резервира
    [Fact]
    public void CanBeBooked_WhenCopyIsAvailableAndGood_ReturnsTrue()
    {
        var copy = new GameCopy
        {
            InventoryCode = "BG-001-02",
            Condition = GameCondition.Good,
            Status = GameCopyStatus.Available
        };

        bool canBeBooked = copy.CanBeBooked();

        Assert.True(canBeBooked);
    }

    // Тест 3: опит за заемане на вече заета игра хвърля грешка (InvalidOperationException)
    [Fact]
    public void MarkAsInUse_WhenAlreadyInUse_ThrowsInvalidOperationException()
    {
        var copy = new GameCopy
        {
            InventoryCode = "BG-001-03",
            Condition = GameCondition.New,
            Status = GameCopyStatus.InUse
        };

        Assert.Throws<InvalidOperationException>(() => copy.MarkAsInUse());
    }

    // Тест 4: при връщане в добро състояние екземплярът отново става Available
    [Fact]
    public void MarkAsReturned_WhenConditionIsGood_SetsStatusToAvailable()
    {
        var copy = new GameCopy
        {
            InventoryCode = "BG-001-04",
            Condition = GameCondition.Good,
            Status = GameCopyStatus.InUse
        };

        copy.MarkAsReturned(GameCondition.Good);

        Assert.Equal(GameCopyStatus.Available, copy.Status);
        Assert.Equal(GameCondition.Good, copy.Condition);
    }

    // Тест 5: при връщане със счупени части екземплярът става Damaged
    [Fact]
    public void MarkAsReturned_WhenConditionIsDamaged_SetsStatusToDamaged()
    {
        var copy = new GameCopy
        {
            InventoryCode = "BG-001-05",
            Condition = GameCondition.Good,
            Status = GameCopyStatus.InUse
        };

        copy.MarkAsReturned(GameCondition.Damaged);

        Assert.Equal(GameCopyStatus.Damaged, copy.Status);
    }

    // Тест 6: 15-минутен гратисен период при закъснение 
    [Fact]
    public void IsOverdue_Respects15MinuteGracePeriod()
    {
        var dueTime = new DateTime(2026, 11, 21, 18, 0, 0, DateTimeKind.Utc);
        var loan = new Loan
        {
            LentAt = dueTime.AddHours(-2),
            DueAt = dueTime,
            Status = LoanStatus.Active
        };

        // 10 минути след крайния час - още е в гратисния период (не е Overdue)
        bool overdueAfter10Min = loan.IsOverdue(dueTime.AddMinutes(10));
        // 16 минути след крайния час - минали са над 15 минути (вече е Overdue)
        bool overdueAfter16Min = loan.IsOverdue(dueTime.AddMinutes(16));

        Assert.False(overdueAfter10Min);
        Assert.True(overdueAfter16Min);
    }

    // Тест 7: проверка за поддържан брой играчи в класа Game
    [Fact]
    public void SupportsPlayerCount_ChecksMinAndMaxPlayers()
    {
        var game = new Game
        {
            Title = "Catan",
            MinPlayers = 3,
            MaxPlayers = 4,
            IsActive = true
        };

        Assert.False(game.SupportsPlayerCount(2));
        Assert.True(game.SupportsPlayerCount(3));
        Assert.True(game.SupportsPlayerCount(4));
        Assert.False(game.SupportsPlayerCount(5));
    }
}
