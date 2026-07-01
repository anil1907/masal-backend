using Core.Repositories;
using Domain.Entities.Users;

namespace Domain.Entities.Notifications;

/// An APNs device token registered by an installed iOS app, scoped to a user.
/// One row per (UserId, Token). Re-registering an existing token just refreshes it.
public class DeviceToken : Entity
{
    public long UserId { get; set; }
    public virtual User User { get; set; } = default!;

    /// The hex APNs device token from the iOS client.
    public string Token { get; set; } = default!;
    /// "ios" today; kept for future platforms.
    public string Platform { get; set; } = "ios";
    /// true => production APNs gateway (TestFlight/App Store build); false => sandbox (debug build).
    public bool IsProduction { get; set; } = true;
}
