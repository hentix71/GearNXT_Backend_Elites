using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace GearNXT_Backend.Services;

public class EmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        var fromEmail = GetSetting("Smtp:FromEmail", "Email:FromEmail");
        var fromName = GetSetting("Smtp:FromName", "Email:FromName") ?? "GearNXT";
        var smtpHost = GetSetting("Smtp:Host", "Email:SmtpHost");
        var smtpPort = GetIntSetting("Smtp:Port", "Email:SmtpPort", 587);
        var username = GetSetting("Smtp:Username", "Email:Username");
        var password = GetSetting("Smtp:Password", "Email:Password");
        var useSsl = GetBoolSetting("Smtp:EnableSsl", "Email:UseSsl", true);

        if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogInformation("[EmailService] Email config missing. Simulating email to {To}: {Subject}", to, subject);
            return Task.CompletedTask;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        return SendAsync(message, smtpHost, smtpPort, useSsl, username, password);
    }

    public Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachmentBytes, string attachmentFileName, string? attachmentContentType = "application/pdf")
    {
        var fromEmail = GetSetting("Smtp:FromEmail", "Email:FromEmail");
        var fromName = GetSetting("Smtp:FromName", "Email:FromName") ?? "GearNXT";
        var smtpHost = GetSetting("Smtp:Host", "Email:SmtpHost");
        var smtpPort = GetIntSetting("Smtp:Port", "Email:SmtpPort", 587);
        var username = GetSetting("Smtp:Username", "Email:Username");
        var password = GetSetting("Smtp:Password", "Email:Password");
        var useSsl = GetBoolSetting("Smtp:EnableSsl", "Email:UseSsl", true);

        if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogInformation("[EmailService] Email config missing. Simulating email to {To}: {Subject} (with attachment {File})", to, subject, attachmentFileName);
            return Task.CompletedTask;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder();
        builder.TextBody = body;
        if (attachmentBytes != null && attachmentBytes.Length > 0)
        {
            builder.Attachments.Add(attachmentFileName, attachmentBytes, ContentType.Parse(attachmentContentType));
        }

        message.Body = builder.ToMessageBody();

        return SendAsync(message, smtpHost, smtpPort, useSsl, username, password);
    }

    private async Task SendAsync(MimeMessage message, string smtpHost, int smtpPort, bool useSsl, string? username, string? password)
    {
        using var client = new SmtpClient();
        var sslOption = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(smtpHost, smtpPort, sslOption);

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            await client.AuthenticateAsync(username, password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("[EmailService] Email sent to {To}: {Subject}", message.To, message.Subject);
    }

    private string? GetSetting(string primary, string fallback)
    {
        return _configuration[primary] ?? _configuration[fallback];
    }

    private int GetIntSetting(string primary, string fallback, int fallbackValue)
    {
        var raw = GetSetting(primary, fallback);
        return int.TryParse(raw, out var value) ? value : fallbackValue;
    }

    private bool GetBoolSetting(string primary, string fallback, bool fallbackValue)
    {
        var raw = GetSetting(primary, fallback);
        return bool.TryParse(raw, out var value) ? value : fallbackValue;
    }
}
