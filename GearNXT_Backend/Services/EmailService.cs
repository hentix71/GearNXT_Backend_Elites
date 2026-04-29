using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GearNXT_Backend.Services;

public class EmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        // Placeholder — in development we just log the action.
        _logger.LogInformation("[EmailService] Sending email to {To}: {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
