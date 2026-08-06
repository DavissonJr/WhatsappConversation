using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Infrastructure.Services;

/// <summary>
/// Envia e-mail via SMTP genérico (Gmail, SendGrid, Mailgun, etc — qualquer
/// provedor que fale SMTP funciona, é só configurar host/porta/credenciais).
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = _config["Smtp:Host"];
        var username = _config["Smtp:Username"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
        {
            // Sem SMTP configurado (ex: ambiente de desenvolvimento sem credenciais
            // ainda) — loga o e-mail que seria enviado, pra não travar o teste local.
            _logger.LogWarning(
                "SMTP não configurado. E-mail NÃO enviado de verdade. Para={To} Assunto={Subject} Corpo={Body}",
                toEmail, subject, htmlBody);
            return;
        }

        var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
        var password = _config["Smtp:Password"];
        var fromEmail = _config["Smtp:FromEmail"] ?? username;
        var fromName = _config["Smtp:FromName"] ?? "Zappy CRM";
        var useSsl = !bool.TryParse(_config["Smtp:UseSsl"], out var ssl) || ssl;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);
            if (!string.IsNullOrEmpty(password))
                await client.AuthenticateAsync(username, password, ct);
            await client.SendAsync(message, ct);
        }
        finally
        {
            if (client.IsConnected) await client.DisconnectAsync(true, ct);
        }
    }
}
