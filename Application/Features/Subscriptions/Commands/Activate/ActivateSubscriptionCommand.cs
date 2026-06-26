using Application.Features.Subscriptions.Constants;
using Application.Features.Subscriptions.Queries.GetStatus;
using Application.Persistence;
using Application.Services.CurrentUser;
using Application.Services.Store;
using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exception.Types;
using Domain.Entities.Subscriptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Subscriptions.Commands.Activate;

/// Record a store purchase as a server-side entitlement (the source of truth the story gate reads).
/// The transaction is verified first (dev trusts the client; production validates with Apple), then
/// the user's entitlement row is upserted. Returns the refreshed status.
public class ActivateSubscriptionCommand : IRequest<GetSubscriptionStatusResponse>, ISecuredRequest
{
    public string ProductId { get; set; } = default!;
    public string Provider { get; set; } = "apple";
    public string? TransactionId { get; set; }

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class ActivateSubscriptionCommandHandler : IRequestHandler<ActivateSubscriptionCommand, GetSubscriptionStatusResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly IStoreVerifier _storeVerifier;
        private readonly ICurrentUser _currentUser;

        public ActivateSubscriptionCommandHandler(
            IApplicationDbContext db,
            IStoreVerifier storeVerifier,
            ICurrentUser currentUser)
        {
            _db = db;
            _storeVerifier = storeVerifier;
            _currentUser = currentUser;
        }

        public async Task<GetSubscriptionStatusResponse> Handle(ActivateSubscriptionCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();

            StoreVerificationResult verdict = await _storeVerifier.VerifyAsync(
                request.Provider, request.ProductId, request.TransactionId, cancellationToken);
            if (!verdict.Valid)
                throw new BusinessException("Satın alma doğrulanamadı.");

            Entitlement? entitlement = await _db.Entitlements
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (entitlement is null)
            {
                entitlement = new Entitlement
                {
                    UserId = userId,
                    Provider = request.Provider,
                    ProductId = verdict.ProductId,
                    CurrentPeriodEnd = verdict.ExpiresAtUtc,
                    IsActive = true
                };
                _db.Entitlements.Add(entitlement);
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                entitlement.Provider = request.Provider;
                entitlement.ProductId = verdict.ProductId;
                entitlement.CurrentPeriodEnd = verdict.ExpiresAtUtc;
                entitlement.IsActive = true;
                _db.Entitlements.Update(entitlement);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return new GetSubscriptionStatusResponse
            {
                IsPremium = true,
                RenewsAt = entitlement.CurrentPeriodEnd,
                WeeklyFreeLimit = SubscriptionConstants.WeeklyFreeStories,
                RemainingThisWeek = 0
            };
        }
    }
}
