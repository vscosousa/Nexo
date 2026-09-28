using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Options;

namespace Nexo.Api.Infrastructure.Email;

/// <summary>Sends mail through the SMTP server named in <see cref="EmailOptions"/>.</summary>
public class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message)
    {
        var settings = options.Value;
        using var mail = new MailMessage
        {
            From = new MailAddress(settings.From),
            Subject = message.Subject,
            Body = message.Text,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
        };
        mail.To.Add(message.To);
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.Html, Encoding.UTF8, MediaTypeNames.Text.Html));

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            Timeout = settings.TimeoutSeconds * 1000,
        };
        if (!string.IsNullOrEmpty(settings.Username))
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        await client.SendMailAsync(mail);
    }
}
