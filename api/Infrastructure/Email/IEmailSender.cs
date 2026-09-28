namespace Nexo.Api.Infrastructure.Email;

/// <summary>A message with a plain-text body and an HTML alternative.</summary>
public record EmailMessage(string To, string Subject, string Text, string Html);

public interface IEmailSender
{
    /// <summary>Delivers the message; throws when it cannot be handed to the mail server.</summary>
    Task SendAsync(EmailMessage message);
}
