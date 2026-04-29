using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
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
        var fromEmail = _configuration["Email:FromEmail"];
        var smtpHost = _configuration["Email:SmtpHost"];
        var smtpPort = int.TryParse(_configuration["Email:SmtpPort"], out var port) ? port : 587;
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var useSsl = bool.TryParse(_configuration["Email:UseSsl"], out var ssl) && ssl;

        if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogInformation("[EmailService] Email config missing. Simulating email to {To}: {Subject}", to, subject);
            return Task.CompletedTask;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_configuration["Email:FromName"] ?? "GearNXT", fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        return SendAsync(message, smtpHost, smtpPort, useSsl, username, password);
    }

    private async Task SendAsync(MimeMessage message, string smtpHost, int smtpPort, bool useSsl, string? username, string? password)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort, useSsl);

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            await client.AuthenticateAsync(username, password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("[EmailService] Email sent to {To}: {Subject}", message.To, message.Subject);
    }
}
