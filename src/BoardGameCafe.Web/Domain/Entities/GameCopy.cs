using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Domain.Entities;

public class GameCopy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;
    public string InventoryCode { get; set; } = string.Empty;
    public GameCondition Condition { get; set; } = GameCondition.New;
    public GameCopyStatus Status { get; set; } = GameCopyStatus.Available;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<ReservationGame> Reservations { get; set; } = new List<ReservationGame>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();


    public bool CanBeBooked()
    {
        return Status == GameCopyStatus.Available
            && Condition != GameCondition.Damaged;
    }

    public void MarkAsInUse()
    {
        if (Status == GameCopyStatus.Damaged || Status == GameCopyStatus.Retired || Condition == GameCondition.Damaged)
        {
            throw new InvalidOperationException("Повреден или бракуван екземпляр не може да бъде зает.");
        }

        if (Status == GameCopyStatus.InUse)
        {
            throw new InvalidOperationException("Този екземпляр се използва на маса.");
        }

        Status = GameCopyStatus.InUse;
    }

    public void MarkAsReturned(GameCondition conditionAfterReturn)
    {
        Condition = conditionAfterReturn;

        if (conditionAfterReturn == GameCondition.Damaged)
        {
            Status = GameCopyStatus.Damaged;
        }
        else
        {
            Status = GameCopyStatus.Available;
        }
    }

    public void MarkAsDamaged()
    {
        Condition = GameCondition.Damaged;
        Status = GameCopyStatus.Damaged;
    }
}

