using BoardGameCafe.Web.Domain.Enums;

namespace BoardGameCafe.Web.Domain.Entities;

public class EmailOutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public int Attempts { get; set; }
    public string LastError { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}
