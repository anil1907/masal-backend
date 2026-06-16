namespace Application.Services.EmailService;

public abstract class EmailServiceBase
{
    public abstract Task SendEmailAsync(string to, string subject, string body);
    public abstract Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachmentData, string attachmentFileName, string contentType);
}