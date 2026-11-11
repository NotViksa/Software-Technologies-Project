using BoardGameCafe.Web.Application.Dtos;

namespace BoardGameCafe.Web.Application.Abstractions;

public interface IEmailQueue
{
    Task EnqueueAsync(EmailMessage message, CancellationToken ct);
}
