using Application.Persistence;
using Application.Services.CurrentUser;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Account.Commands.DeleteAccount;

// Self-service account deletion (App Store Guideline 5.1.1(v)): the authenticated user
// permanently deletes their own account and ALL associated data. Owner-scoped: only ever
// touches rows belonging to the current user.
public class DeleteAccountCommand : IRequest<DeletedAccountResponse>, ISecuredRequest, ILoggableRequest
{
    // Any authenticated user may delete their own account (the controller's [Authorize] enforces auth).
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, DeletedAccountResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;

        public DeleteAccountCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<DeletedAccountResponse> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();

            // Story data is scoped through the user's children; delete leaf rows first so
            // foreign keys never block the parent deletes.
            List<long> childIds = await _db.Children
                .Where(c => c.UserId == userId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            if (childIds.Count > 0)
            {
                await _db.StoryChapters.Where(s => childIds.Contains(s.ChildId)).ExecuteDeleteAsync(cancellationToken);
                await _db.StorySeries.Where(s => childIds.Contains(s.ChildId)).ExecuteDeleteAsync(cancellationToken);
            }
            await _db.StoryGenerationLogs.Where(l => l.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _db.Children.Where(c => c.UserId == userId).ExecuteDeleteAsync(cancellationToken);

            // Account-scoped data.
            await _db.Entitlements.Where(e => e.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _db.RefreshTokens.Where(r => r.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _db.UserOperationClaims.Where(uoc => uoc.UserId == userId).ExecuteDeleteAsync(cancellationToken);

            // OTP rows are keyed by phone number, not user id.
            string? phone = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.PhoneNumber)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrEmpty(phone))
                await _db.PhoneOtps.Where(o => o.PhoneNumber == phone).ExecuteDeleteAsync(cancellationToken);

            // Finally the user record itself.
            await _db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync(cancellationToken);

            return new DeletedAccountResponse { Deleted = true };
        }
    }
}
