namespace Application.Services.Push;

/// Sends Apple Push Notifications to a user's registered devices.
public interface IPushSender
{
    /// Send an alert push to every active device registered for the user.
    /// No-ops silently if APNs is not configured or the user has no devices.
    Task SendToUserAsync(long userId, string title, string body, CancellationToken cancellationToken = default);
}
