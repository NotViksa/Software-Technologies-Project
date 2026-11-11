namespace BoardGameCafe.Web.Application.Dtos;

public record EmailMessage(string To, string Subject, string Body, bool IsHtml = true);
