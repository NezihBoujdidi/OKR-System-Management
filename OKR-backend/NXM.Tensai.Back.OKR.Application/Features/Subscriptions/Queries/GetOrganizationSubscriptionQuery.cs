using MediatR;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using NXM.Tensai.Back.OKR.Application;

namespace NXM.Tensai.Back.OKR.Application.Features.Subscriptions.Queries
{
    public record GetOrganizationSubscriptionQuery(Guid OrganizationId) : IRequest<SubscriptionDto?>;

    public class GetOrganizationSubscriptionQueryHandler : IRequestHandler<GetOrganizationSubscriptionQuery, SubscriptionDto?>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public GetOrganizationSubscriptionQueryHandler(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<SubscriptionDto?> Handle(GetOrganizationSubscriptionQuery request, CancellationToken cancellationToken)
        {
            var subscription = await _subscriptionRepository.GetByOrganizationIdAsync(request.OrganizationId);
            if (subscription == null)
                return null;

            return subscription.ToDto();
        }
    }
}
