using NXM.Tensai.Back.OKR.Application;
public class GetBillingHistoryQuery : IRequest<List<BillingHistoryItemDto>>
{
    public Guid OrganizationId { get; set; }
    public GetBillingHistoryQuery(Guid organizationId) => OrganizationId = organizationId;
}
public class GetBillingHistoryQueryHandler : IRequestHandler<GetBillingHistoryQuery, List<BillingHistoryItemDto>>
{
    private readonly ISubscriptionService _subscriptionService;
    public GetBillingHistoryQueryHandler(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    public async Task<List<BillingHistoryItemDto>> Handle(GetBillingHistoryQuery request, CancellationToken cancellationToken)
    {
        // Implement logic to fetch billing history from Stripe or your DB
        return await _subscriptionService.GetBillingHistoryAsync(request.OrganizationId);
    }
}
