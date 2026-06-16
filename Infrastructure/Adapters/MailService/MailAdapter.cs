using Application.Services.EmailService;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;

namespace Infrastructure.Adapters.MailService;

public class MailAdapter : EmailServiceBase
{
    private readonly MailSettings _mailSettings;

    public MailAdapter(IOptions<MailSettings> mailSettings)
    {
        _mailSettings = mailSettings.Value;
    }

    public override async Task SendEmailAsync(string to, string subject, string body)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Email));
        email.To.Add(new MailboxAddress("", to));
        email.Subject = subject;
        email.Body = new TextPart("html") { Text = body };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_mailSettings.Email, _mailSettings.Password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    public override async Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachmentData, string attachmentFileName, string contentType)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Email));
        email.To.Add(new MailboxAddress("", to));
        email.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = body
        };

        builder.Attachments.Add(attachmentFileName, attachmentData, ContentType.Parse(contentType));

        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_mailSettings.Email, _mailSettings.Password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}