using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Infrastructure.Persistence;
using Stripe;
using DomainSubscription = NXM.Tensai.Back.OKR.Domain.Subscription;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class SubscriptionService : ISubscriptionService
{
    private readonly OKRDbContext _dbContext;
    private readonly StripeSettings _stripeSettings;

    public SubscriptionService(
        OKRDbContext dbContext,
        IOptions<StripeSettings> stripeSettings)
    {
        _dbContext = dbContext;
        _stripeSettings = stripeSettings.Value;
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    public async Task<SubscriptionResponseDTO> CreateSubscriptionAsync(CreateSubscriptionRequestDTO request)
    {
        // Validate request
        if (string.IsNullOrEmpty(request.PlanId))
        {
            throw new ArgumentException("Plan ID is required");
        }

        // Validate payment method ID
        if (string.IsNullOrEmpty(request.PaymentMethodId))
        {
            throw new ArgumentException("Payment method ID is required");
        }

        // Find plan in database
        var plan = await _dbContext.Set<SubscriptionPlanEntity>()
            .FirstOrDefaultAsync(p => p.PlanId.ToLower() == request.PlanId.ToLower() && p.IsActive);

        if (plan == null)
        {
            throw new ArgumentException("Invalid or inactive plan selected");
        }

        // Check if organization already has an active subscription
        var existingSubscription = await _dbContext.Set<DomainSubscription>()
            .FirstOrDefaultAsync(s => s.OrganizationId == request.OrganizationId && s.IsActive);

        if (existingSubscription != null)
        {
            throw new InvalidOperationException("Organization already has an active subscription");
        }

        // Get payment method
        var paymentMethodService = new PaymentMethodService();
        var paymentMethod = await paymentMethodService.GetAsync(request.PaymentMethodId);

        // Get or create customer in Stripe
        var customerId = string.Empty;
        var customerService = new CustomerService();

        // Check if the payment method is already attached to a customer
        if (!string.IsNullOrEmpty(paymentMethod.CustomerId))
        {
            // Use the customer associated with the payment method
            customerId = paymentMethod.CustomerId;
        }
        else
        {
            // Create a new customer with minimal info
            var customerOptions = new CustomerCreateOptions
            {
                PaymentMethod = request.PaymentMethodId,
                Metadata = new Dictionary<string, string>
                {
                    { "OrganizationId", request.OrganizationId.ToString() }
                }
            };

            var customer = await customerService.CreateAsync(customerOptions);
            customerId = customer.Id;
        }

        // If the payment method isn't attached to the customer, attach it
        if (string.IsNullOrEmpty(paymentMethod.CustomerId) || paymentMethod.CustomerId != customerId)
        {
            try
            {
                // Attach payment method to customer
                var paymentMethodAttachOptions = new PaymentMethodAttachOptions
                {
                    Customer = customerId
                };
                await paymentMethodService.AttachAsync(request.PaymentMethodId, paymentMethodAttachOptions);
            }
            catch (StripeException ex)
            {
                // Handle the case where the payment method is already attached
                if (ex.StripeError.Code != "payment_method_already_attached")
                {
                    throw;
                }
            }
        }

        // Update customer's default payment method
        var customerUpdateOptions = new CustomerUpdateOptions
        {
            InvoiceSettings = new CustomerInvoiceSettingsOptions
            {
                DefaultPaymentMethod = request.PaymentMethodId
            }
        };
        await customerService.UpdateAsync(customerId, customerUpdateOptions);

        // Create subscription
        var subscriptionOptions = new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = new List<SubscriptionItemOptions>
            {
                new() { Price = plan.StripePriceId }
            },
            PaymentBehavior = "allow_incomplete",
            PaymentSettings = new SubscriptionPaymentSettingsOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                SaveDefaultPaymentMethod = "on_subscription"
            },
            Expand = new List<string> { "latest_invoice.payment_intent" },
            Metadata = new Dictionary<string, string>
            {
                { "OrganizationId", request.OrganizationId.ToString() }
            }
        };

        var subscriptionService = new Stripe.SubscriptionService();
        var stripeSubscription = await subscriptionService.CreateAsync(subscriptionOptions);

        // Create entity
        var subscription = new DomainSubscription
        {
            OrganizationId = request.OrganizationId,
            CreatedByUserId = request.CreatedByUserId,
            StripeCustomerId = customerId,
            StripeSubscriptionId = stripeSubscription.Id,
            Plan = plan.PlanType,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            IsActive = true,
            Status = stripeSubscription.Status,
            Amount = plan.Price,
            Currency = "usd",
            LastPaymentIntentId = stripeSubscription.LatestInvoice?.PaymentIntentId
        };

        _dbContext.Set<DomainSubscription>().Add(subscription);
        await _dbContext.SaveChangesAsync();

        return subscription.ToResponse();
    }

    public async Task<SubscriptionResponseDTO> GetSubscriptionByOrganizationIdAsync(Guid organizationId)
    {
        var subscription = await _dbContext.Set<DomainSubscription>()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.IsActive);

        if (subscription == null)
        {
            throw new KeyNotFoundException("No active subscription found for this organization");
        }

        return subscription.ToResponse();
    }

    public async Task<SubscriptionResponseDTO> CancelSubscriptionAsync(Guid organizationId)
    {
        var subscription = await _dbContext.Set<DomainSubscription>()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.IsActive);

        if (subscription == null)
        {
            throw new KeyNotFoundException("No active subscription found for this organization");
        }

        // Cancel in Stripe
        var subscriptionService = new Stripe.SubscriptionService();
        await subscriptionService.CancelAsync(subscription.StripeSubscriptionId, new SubscriptionCancelOptions());

        // Update local record
        subscription.IsActive = false;
        subscription.Status = "canceled";
        subscription.ModifiedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return subscription.ToResponse();
    }

    public async Task<SubscriptionResponseDTO> UpdateSubscriptionAsync(Guid organizationId, string newPlanId)
    {
        // Find plan in database
        var plan = await _dbContext.Set<SubscriptionPlanEntity>()
            .FirstOrDefaultAsync(p => p.PlanId.ToLower() == newPlanId.ToLower() && p.IsActive);

        if (plan == null)
        {
            throw new ArgumentException("Invalid or inactive plan selected");
        }

        var subscription = await _dbContext.Set<DomainSubscription>()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.IsActive);

        if (subscription == null)
        {
            throw new KeyNotFoundException("No active subscription found for this organization");
        }

        // Update in Stripe
        var subscriptionService = new Stripe.SubscriptionService();
        var stripeSubscription = await subscriptionService.GetAsync(subscription.StripeSubscriptionId);

        // Get the subscription item ID
        var itemId = stripeSubscription.Items.Data[0].Id;

        // Update the subscription
        var subscriptionUpdateOptions = new SubscriptionUpdateOptions
        {
            Items = new List<SubscriptionItemOptions>
            {
                new()
                {
                    Id = itemId,
                    Price = plan.StripePriceId
                }
            }
        };

        await subscriptionService.UpdateAsync(subscription.StripeSubscriptionId, subscriptionUpdateOptions);

        // Update local record
        subscription.Plan = plan.PlanType;
        subscription.Amount = plan.Price;
        subscription.ModifiedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return subscription.ToResponse();
    }
    public async Task<List<BillingHistoryItemDto>> GetBillingHistoryAsync(Guid organizationId)
    {
        // Find the active subscription for the organization
        var subscription = await _dbContext.Set<Domain.Subscription>()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.IsActive);

        if (subscription == null || string.IsNullOrEmpty(subscription.StripeCustomerId))
            return new List<BillingHistoryItemDto>();

        var invoiceService = new InvoiceService();
        // List basic invoice info
        var invoices = await invoiceService.ListAsync(new InvoiceListOptions
        {
            Customer = subscription.StripeCustomerId,
            Limit = 100
        });

        // Fetch full invoice to ensure InvoicePdf is populated
        var result = new List<BillingHistoryItemDto>();
        foreach (var inv in invoices.Data)
        {
            // Retrieve full invoice for PDF URL
            var fullInv = await invoiceService.GetAsync(inv.Id);
            // Determine paidAt timestamp
            DateTime paidAt = fullInv.Status == "paid" && fullInv.StatusTransitions?.PaidAt != null
                ? fullInv.StatusTransitions.PaidAt.Value
                : fullInv.Created;
            result.Add(new BillingHistoryItemDto
            {
                InvoiceId = fullInv.Id,
                PaidAt = paidAt,
                Amount = fullInv.AmountPaid / 100m,
                Currency = fullInv.Currency,
                Status = fullInv.Status,
                Description = fullInv.Description,
                InvoicePdfUrl = fullInv.InvoicePdf
            });
        }

        return result;
    }
public async Task<SuperAdminDashboardDto> GetSuperAdminStatsAsync()
{
    var now = DateTime.UtcNow;
    var monthAgo = now.AddDays(-30);

    // Active subscriptions count
    var subsQuery = _dbContext.Set<Domain.Subscription>().AsQueryable();
    var activeCount = await subsQuery.CountAsync(s => s.IsActive);

    // Monthly Recurring Revenue (sum of plan prices for active subs)
    var mrr = await subsQuery.SumAsync(s => s.Amount);

    // Annual Recurring Revenue
    var arr = mrr * 12m;

    // Average Revenue per User
    var arpu = activeCount > 0 ? mrr / activeCount : 0m;

    // Churn: count canceled in last 30 days
    var canceledLast30 = await _dbContext.Set<Domain.Subscription>()
        .CountAsync(s => !s.IsActive && s.ModifiedDate.HasValue && s.ModifiedDate.Value >= monthAgo);

    // Approximate active at start of window
    var startActive = await _dbContext.Set<Domain.Subscription>()
        .CountAsync(s => s.CreatedDate < monthAgo && (s.ModifiedDate == null || s.ModifiedDate >= monthAgo));

    var churnRate = startActive > 0
        ? (double)canceledLast30 / startActive * 100.0
        : 0.0;

    // Plan distribution among active subscriptions
    var planDist = await _dbContext.Set<Domain.Subscription>()
        .Where(s => s.IsActive)
        .GroupBy(s => s.Plan)
        .Select(g => new PlanDistributionItemDto
        {
            Plan = g.Key.ToString(),
            Count = g.Count()
        })
        .ToListAsync();

    return new SuperAdminDashboardDto
    {
        ActiveSubscriptions = activeCount,
        Mrr = mrr,
        Arr = arr,
        Arpu = arpu,
        ChurnRate = churnRate,
        PlanDistribution = planDist
    };
}
}