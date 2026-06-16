using Application.Services.SmsService;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Adapters.SmsService;

/// <summary>
/// Dev/test SMS sender: logs the message instead of sending a real (paid) SMS.
/// This is the active implementation until a real provider (Netgsm) is wired
/// for production. Swapping providers means adding a new ISmsSender and changing
/// one line in InfrastructureServiceRegistration - no auth-logic changes.
/// </summary>
public class ConsoleSmsSender : ISmsSender
{
    private readonly ILogger<ConsoleSmsSender> _logger;

    public ConsoleSmsSender(ILogger<ConsoleSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV SMS] To: {PhoneNumber} | Message: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}
