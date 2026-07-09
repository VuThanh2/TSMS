using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace SharedInfrastructure.Email;

// Dùng System.Net.Mail.SmtpClient built-in
public sealed class SmtpEmailSender : IEmailSender {
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options) {
        _options = options.Value;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) {
        using var client = new SmtpClient(_options.Host, _options.Port) {
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            EnableSsl = _options.EnableSsl
        };

        using var mail = new MailMessage {
            From = new MailAddress(_options.SenderEmail, _options.SenderName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = false
        };
        mail.To.Add(message.To);

        await client.SendMailAsync(mail, cancellationToken);
    }
}