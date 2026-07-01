using Application.Features.Subscriptions.Constants;
using Application.Persistence;
using Application.Services.CurrentUser;
using Core.Application.Pipelines.Authorization;
using Domain.Entities.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Notifications.Commands.RegisterDevice;

/// Register (or refresh) the calling user's APNs device token so the server can push them
/// when their story is ready. Idempotent: a token already on file is re-pointed to this user.
public class RegisterDeviceCommand : IRequest<RegisterDeviceResponse>, ISecuredRequest
{
    public string Token { get; set; } = default!;
    public string Platform { get; set; } = "ios";
    /// true => production APNs (TestFlight/App Store build); false => sandbox (debug build).
    public bool IsProduction { get; set; } = true;

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, RegisterDeviceResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;

        public RegisterDeviceCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<RegisterDeviceResponse> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();

            DeviceToken? existing = await _db.DeviceTokens
                .FirstOrDefaultAsync(d => d.Token == request.Token, cancellationToken);

            if (existing is null)
            {
                _db.DeviceTokens.Add(new DeviceToken
                {
                    UserId = userId,
                    Token = request.Token,
                    Platform = request.Platform,
                    IsProduction = request.IsProduction
                });
            }
            else
            {
                existing.UserId = userId;
                existing.Platform = request.Platform;
                existing.IsProduction = request.IsProduction;
                _db.DeviceTokens.Update(existing);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new RegisterDeviceResponse { Registered = true };
        }
    }
}

public class RegisterDeviceResponse
{
    public bool Registered { get; set; }
}
