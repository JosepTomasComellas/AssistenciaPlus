using AssistenciaPlus.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace AssistenciaPlus.Infrastructure.Email;

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(
        string toEmail, string userName, string password, string appUrl, CancellationToken ct = default)
    {
        var subject = "Benvingut/da a AssistenciaPlus";
        var body = $"""
            <html><body style="font-family: Arial, sans-serif; color: #333;">
            <div style="max-width:600px; margin:auto; padding:20px;">
                <h2 style="color:#1976D2;">Benvingut/da a AssistenciaPlus</h2>
                <p>Hola <strong>{userName}</strong>,</p>
                <p>El teu compte ha estat creat. Aquí tens les teves credencials d'accés:</p>
                <div style="background:#f5f5f5; padding:15px; border-radius:8px; margin:20px 0;">
                    <p><strong>Adreça d'accés:</strong> <a href="{appUrl}">{appUrl}</a></p>
                    <p><strong>Usuari:</strong> {toEmail}</p>
                    <p><strong>Contrasenya temporal:</strong> <code style="background:#e0e0e0;padding:3px 8px;border-radius:4px;">{password}</code></p>
                </div>
                <p style="color:#F44336;"><strong>Important:</strong> Canvia la teva contrasenya en el primer accés.</p>
                <hr style="border-color:#e0e0e0;"/>
                <p style="font-size:12px; color:#999;">AssistenciaPlus - Sistema de gestió d'assistència</p>
            </div>
            </body></html>
            """;
        await SendGenericEmailAsync(toEmail, subject, body, ct);
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail, string userName, string resetToken, string appUrl, CancellationToken ct = default)
    {
        var resetUrl = $"{appUrl}/reset-password?token={resetToken}";
        var subject = "Restabliment de contrasenya - AssistenciaPlus";
        var body = $"""
            <html><body style="font-family: Arial, sans-serif; color: #333;">
            <div style="max-width:600px; margin:auto; padding:20px;">
                <h2 style="color:#1976D2;">Restabliment de contrasenya</h2>
                <p>Hola <strong>{userName}</strong>,</p>
                <p>Has sol·licitat restablir la teva contrasenya. Fes clic al botó per continuar:</p>
                <div style="text-align:center; margin:30px 0;">
                    <a href="{resetUrl}" style="background:#1976D2;color:white;padding:12px 24px;
                       border-radius:6px;text-decoration:none;font-weight:bold;">
                        Restablir contrasenya
                    </a>
                </div>
                <p style="color:#999;font-size:13px;">
                    Aquest enllaç caducarà en 2 hores. Si no has fet aquesta sol·licitud, ignora aquest correu.
                </p>
                <hr style="border-color:#e0e0e0;"/>
                <p style="font-size:12px; color:#999;">AssistenciaPlus - Sistema de gestió d'assistència</p>
            </div>
            </body></html>
            """;
        await SendGenericEmailAsync(toEmail, subject, body, ct);
    }

    public async Task SendAttendanceReminderAsync(
        string toEmail, string userName, string appUrl, CancellationToken ct = default)
    {
        var subject = "Recordatori d'assistència - AssistenciaPlus";
        var body = $"""
            <html><body style="font-family: Arial, sans-serif; color: #333;">
            <div style="max-width:600px; margin:auto; padding:20px;">
                <h2 style="color:#1976D2;">Recordatori d'assistència</h2>
                <p>Hola <strong>{userName}</strong>,</p>
                <p>Tens sessions pendents de passar llista. Accedeix a l'aplicació per completar-les:</p>
                <div style="text-align:center; margin:30px 0;">
                    <a href="{appUrl}" style="background:#1976D2;color:white;padding:12px 24px;
                       border-radius:6px;text-decoration:none;font-weight:bold;">
                        Accedir a AssistenciaPlus
                    </a>
                </div>
            </div>
            </body></html>
            """;
        await SendGenericEmailAsync(toEmail, subject, body, ct);
    }

    public async Task SendGenericEmailAsync(
        string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_settings.User, _settings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var from = new MailAddress(_settings.FromEmail, _settings.FromName);
            var to = new MailAddress(toEmail);

            using var message = new MailMessage(from, to)
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Correu enviat a {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviant correu a {Email}", toEmail);
            throw;
        }
    }
}
