using System.Net;
using System.Net.Mail;
using UserManagement.Models;

namespace UserManagement.Services;

public class EmailService
{
    private readonly EmailSettings _settings;

    // Binding structural SMTP provider credentials on startup
    public EmailService(EmailSettings settings)
    {
        _settings = settings;
    }

    /// Dispatches an out-of-band authentication message housing a unique confirmation identity payload.

    public async Task SendVerificationEmail(
        string recipientEmail,
        string verificationLink)
    {
        // Instantiate an SMTP client context using explicit parameters
        using var client = new SmtpClient(
            _settings.SmtpServer,
            _settings.Port);

        // Enforce Transport Layer Security (TLS/SSL) for transactional email payloads
        client.EnableSsl = true;

        // Attach secure client identity handshake mappings
        client.Credentials = new NetworkCredential(
            _settings.SenderEmail,
            _settings.SenderPassword);

        // Construct the physical email envelope structure
        var message = new MailMessage(
            _settings.SenderEmail,
            recipientEmail);

        message.Subject = "Verify Your Account";

        // Embed the dynamic routing target mapping back to AccountController/VerifyEmail
        message.Body = $"Click the following link to verify your account:\n\n{verificationLink}";

   
        await client.SendMailAsync(message);
    }
}