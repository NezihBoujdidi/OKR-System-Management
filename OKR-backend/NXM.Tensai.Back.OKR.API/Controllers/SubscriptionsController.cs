using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Application.Features.Subscriptions.Queries;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Infrastructure;
using NXM.Tensai.Back.OKR.Infrastructure.Persistence;
using Stripe;
using System.Security.Claims;

namespace NXM.Tensai.Back.OKR.API;

[Route("api/subscriptions")]
[ApiController]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ISubscriptionPlanService _subscriptionPlanService;
    private readonly StripeSettings _stripeSettings;
    private readonly ILogger<SubscriptionsController> _logger;
    private readonly OKRDbContext _dbContext;
    private readonly IMediator _mediator;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ISubscriptionPlanService subscriptionPlanService,
        StripeSettings stripeSettings,
        OKRDbContext dbContext,
        ILogger<SubscriptionsController> logger,
        IMediator mediator)
    {
        _subscriptionService = subscriptionService;
        _subscriptionPlanService = subscriptionPlanService;
        _stripeSettings = stripeSettings;
        _dbContext = dbContext;
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet("config")]
    [AllowAnonymous]
    public IActionResult GetConfig()
    {
        return Ok(new { PublishableKey = _stripeSettings.PublishableKey });
    }

    [HttpGet("organization-subscription")]
    public async Task<IActionResult> GetOrganizationSubscription()
    {
        var organizationId = await GetUserOrganizationId();
        if (organizationId == Guid.Empty)
        {
            return BadRequest("User is not associated with an organization");
        }

        try
        {
            var subscription = await _subscriptionService.GetSubscriptionByOrganizationIdAsync(organizationId);
            return Ok(subscription);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("No active subscription found for this organization");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while retrieving the subscription");
        }
    }

    [HttpPost]
    // [Authorize(Policy = "OrganizationAdmin")] - Uncomment after testing
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequestDTO request)
    {
        var userId = GetUserId();
        var organizationId = await GetUserOrganizationId();

        if (organizationId == Guid.Empty)
        {
            return BadRequest("User is not associated with an organization");
        }

        // Check if the user is an admin of the organization
        var isAdmin = await IsOrganizationAdmin(userId, organizationId);
        if (!isAdmin)
        {
            return Forbid("Only organization administrators can manage subscriptions");
        }

        // Populate the request object
        request.OrganizationId = organizationId;
        request.CreatedByUserId = userId;

        try
        {
            var subscription = await _subscriptionService.CreateSubscriptionAsync(request);
            return Ok(subscription);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while creating the subscription");
        }
    }

    [HttpPut("update-plan")]
    // [Authorize(Policy = "OrganizationAdmin")] - Uncomment after testing
    public async Task<IActionResult> UpdateSubscriptionPlan([FromBody] string newPlanId)
    {
        var userId = GetUserId();
        var organizationId = await GetUserOrganizationId();

        if (organizationId == Guid.Empty)
        {
            return BadRequest("User is not associated with an organization");
        }

        // Check if the user is an admin of the organization
        var isAdmin = await IsOrganizationAdmin(userId, organizationId);
        if (!isAdmin)
        {
            return Forbid("Only organization administrators can manage subscriptions");
        }

        try
        {
            var subscription = await _subscriptionService.UpdateSubscriptionAsync(organizationId, newPlanId);
            return Ok(subscription);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while updating the subscription");
        }
    }

    [HttpPost("cancel")]
    // [Authorize(Policy = "OrganizationAdmin")] - Uncomment after testing
    public async Task<IActionResult> CancelSubscription()
    {
        var userId = GetUserId();
        var organizationId = await GetUserOrganizationId();

        if (organizationId == Guid.Empty)
        {
            return BadRequest("User is not associated with an organization");
        }

        // Check if the user is an admin of the organization
        var isAdmin = await IsOrganizationAdmin(userId, organizationId);
        if (!isAdmin)
        {
            return Forbid("Only organization administrators can manage subscriptions");
        }

        try
        {
            var subscription = await _subscriptionService.CancelSubscriptionAsync(organizationId);
            return Ok(subscription);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while canceling the subscription");
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeSettings.WebhookSecret
            );

            // Handle the event based on its type
            switch (stripeEvent.Type)
            {
                case "invoice.payment_succeeded":
                    // Payment was successful
                    _logger.LogInformation("Payment succeeded event received");
                    break;
                case "customer.subscription.deleted":
                    // Subscription was canceled
                    _logger.LogInformation("Subscription deleted event received");
                    break;
                case "invoice.payment_failed":
                    // Payment failed
                    _logger.LogWarning("Payment failed event received");
                    break;
                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return BadRequest();
        }
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetSubscriptionPlans()
    {
        try
        {
            var plans = await _subscriptionPlanService.GetAllPlansAsync();
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plans");
            return StatusCode(500, "An error occurred while retrieving subscription plans");
        }
    }

    [HttpGet("is-subscribed/{organizationId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> IsSubscribed(Guid organizationId)
    {
        // Use MediatR to send the query
        var result = await _mediator.Send(new GetOrganizationSubscriptionQuery(organizationId));
        // If found, return DTO, else return null
        return Ok(result);
    }
    [HttpGet("billing-history")]
    public async Task<IActionResult> GetBillingHistory()
    {
        var userId = GetUserId();
        var organizationId = await GetUserOrganizationId();
        if (organizationId == Guid.Empty)
            return BadRequest("User is not associated with an organization");

        var isAdmin = await IsOrganizationAdmin(userId, organizationId);
        if (!isAdmin)
            return Forbid("Only organization administrators can view billing history");

        var history = await _mediator.Send(new GetBillingHistoryQuery(organizationId));
        return Ok(history);
    }
    // [Authorize(Policy = "SuperAdmin")]
    [HttpGet("admin/dashboard")]
    public async Task<IActionResult> GetSuperAdminDashboard()
    {
    var dto = await _mediator.Send(new GetDashboardStatsQuery());
    return Ok(dto);
    }
    private Guid GetUserId()
    {
        try
        {
            // Find all nameidentifier claims
            var nameIdentifierClaims = User.Claims
                .Where(c => c.Type == ClaimTypes.NameIdentifier)
                .Select(c => c.Value)
                .ToList();

            // If there are multiple nameidentifier claims, use the last one 
            // (which should be the one added by your middleware)
            if (nameIdentifierClaims.Count > 1)
            {
                var lastNameId = nameIdentifierClaims.Last();
                if (Guid.TryParse(lastNameId, out var userId))
                {
                    _logger.LogInformation("Using last nameidentifier claim as user ID: {UserId}", userId);
                    return userId;
                }
            }

            // Log all available claims for debugging
            _logger.LogWarning("Couldn't find correct user ID, checking all claims:");
            foreach (var claim in User.Claims)
            {
                _logger.LogWarning("  Claim {Type}: {Value}", claim.Type, claim.Value);
            }

            // Fall back to test ID if all else fails
            _logger.LogWarning("No User Id was found!");
            return Guid.Parse("e427348c-a4d4-4575-bee1-ca513b515f4b");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUserId method");
            return Guid.Parse("e427348c-a4d4-4575-bee1-ca513b515f4b");
        }
    }

    private async Task<Guid> GetUserOrganizationId()
    {
        var userId = GetUserId();
        var user = await _dbContext.Users.FindAsync(userId);

        if (user == null || !user.OrganizationId.HasValue)
        {
            return Guid.Empty;
        }

        return user.OrganizationId.Value;
    }

    private async Task<bool> IsOrganizationAdmin(Guid userId, Guid organizationId)
    {
        var user = await _dbContext.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId);
        if (user == null)
        {
            return false;
        }
        // Find the OrganizationAdmin role
        var adminRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name.ToLower() == "organizationadmin");
            
        if (adminRole == null)
        {
            return false;
        }

        // Check if the user has the admin role
        var hasAdminRole = await _dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == adminRole.Id);
            
        return hasAdminRole;

        // Actual implementation would look like:
        /*
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null || user.OrganizationId != organizationId)
        {
            return false;
        }

        // Check if user has organization admin role
        var userRoles = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var adminRoleId = await _dbContext.Roles
            .Where(r => r.Name == "OrganizationAdmin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        return userRoles.Contains(adminRoleId);
        */
    }
    
}