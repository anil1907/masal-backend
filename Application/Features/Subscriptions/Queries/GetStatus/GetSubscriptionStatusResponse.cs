using Core.Application.Responses;

namespace Application.Features.Subscriptions.Queries.GetStatus;

public class GetSubscriptionStatusResponse : IResponse
{
    public bool IsPremium { get; set; }
    public DateTime? RenewsAt { get; set; }          // when premium
    public int WeeklyFreeLimit { get; set; }
    public int RemainingThisWeek { get; set; }       // when free
}
