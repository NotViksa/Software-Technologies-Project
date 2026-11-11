namespace BoardGameCafe.Web.Application.Dtos;

public class AvailabilityResult
{
    public bool IsAvailable { get; set; }
    public Guid? TableId { get; set; }
    public string? TableName { get; set; }
    public Guid? GameCopyId { get; set; }
    public string? GameCopyCode { get; set; }
    public string? Reason { get; set; }

    public static AvailabilityResult Available(Guid tableId, string tableName, Guid gameCopyId, string gameCopyCode)
    {
        return new AvailabilityResult
        {
            IsAvailable = true,
            TableId = tableId,
            TableName = tableName,
            GameCopyId = gameCopyId,
            GameCopyCode = gameCopyCode
        };
    }

    public static AvailabilityResult Unavailable(string reason)
    {
        return new AvailabilityResult { IsAvailable = false, Reason = reason };
    }
}
