namespace Application.Services.SmsService;

/// <summary>
/// SMS transport abstraction. The OTP flow depends only on this interface,
/// so the provider can be swapped without touching auth logic.
/// Dev/test: ConsoleSmsSender (logs the message, no cost, no real SMS).
/// Production: a Netgsm adapter implements this same interface.
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
