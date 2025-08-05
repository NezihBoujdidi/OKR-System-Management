using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Infrastructure.Persistence;
using Stripe;
using DomainSubscription = NXM.Tensai.Back.OKR.Domain.Subscription;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly OKRDbContext _dbContext;
    private readonly StripeSettings _stripeSettings;

    public SubscriptionPlanService(
        OKRDbContext dbContext,
        IOptions<StripeSettings> stripeSettings)
    {
        _dbContext = dbContext;
        _stripeSettings = stripeSettings.Value;
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    public async Task<List<SubscriptionPlanDTO>> GetAllPlansAsync(bool includeInactive = false)
    {
        var query = _dbContext.Set<SubscriptionPlanEntity>()
            .Include(p => p.Features)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        var plans = await query.ToListAsync();
        return plans.Select(MapToDTO).ToList();
    }

    public async Task<SubscriptionPlanDTO> GetPlanByIdAsync(Guid id)
    {
        var plan = await _dbContext.Set<SubscriptionPlanEntity>()
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            throw new KeyNotFoundException($"Subscription plan with ID {id} not found");
        }

        return MapToDTO(plan);
    }

    public async Task<SubscriptionPlanDTO> GetPlanByPlanIdAsync(string planId)
    {
        var plan = await _dbContext.Set<SubscriptionPlanEntity>()
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.PlanId.ToLower() == planId.ToLower() && p.IsActive);

        if (plan == null)
        {
            throw new KeyNotFoundException($"Active subscription plan with ID {planId} not found");
        }

        return MapToDTO(plan);
    }

    public async Task<SubscriptionPlanDTO> CreatePlanAsync(CreateSubscriptionPlanDTO dto)
    {
        // Check if a plan with the same planId already exists
        var existingPlan = await _dbContext.Set<SubscriptionPlanEntity>()
            .FirstOrDefaultAsync(p => p.PlanId.ToLower() == dto.PlanId.ToLower());

        if (existingPlan != null)
        {
            throw new InvalidOperationException($"A subscription plan with ID {dto.PlanId} already exists");
        }

        // Parse the plan type from string
        if (!Enum.TryParse<SubscriptionPlan>(dto.PlanType, out var planType))
        {
            throw new ArgumentException($"Invalid plan type: {dto.PlanType}");
        }

        // Create Stripe product
        var productOptions = new ProductCreateOptions
        {
            Name = dto.Name,
            Description = dto.Description,
            Active = true,
            Metadata = new Dictionary<string, string>
            {
                { "PlanId", dto.PlanId }
            }
        };

        var productService = new ProductService();
        var product = await productService.CreateAsync(productOptions);

        // Create Stripe price
        var priceOptions = new PriceCreateOptions
        {
            Product = product.Id,
            UnitAmount = (long)(dto.Price * 100), // Convert to cents
            Currency = "usd",
            Recurring = new PriceRecurringOptions
            {
                Interval = dto.Interval
            }
        };

        var priceService = new PriceService();
        var price = await priceService.CreateAsync(priceOptions);

        // Create entity
        var plan = new SubscriptionPlanEntity
        {
            PlanId = dto.PlanId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Interval = dto.Interval,
            PlanType = planType,
            StripeProductId = product.Id,
            StripePriceId = price.Id,
            IsActive = true,
            Features = dto.Features.Select(f => new SubscriptionPlanFeature
            {
                Description = f
            }).ToList()
        };

        _dbContext.Set<SubscriptionPlanEntity>().Add(plan);
        await _dbContext.SaveChangesAsync();

        return MapToDTO(plan);
    }

    public async Task<SubscriptionPlanDTO> UpdatePlanAsync(Guid id, UpdateSubscriptionPlanDTO dto)
    {
        var plan = await _dbContext.Set<SubscriptionPlanEntity>()
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            throw new KeyNotFoundException($"Subscription plan with ID {id} not found");
        }

        // Parse the plan type from string if provided
        SubscriptionPlan? planType = null;
        if (!string.IsNullOrEmpty(dto.PlanType) && Enum.TryParse<SubscriptionPlan>(dto.PlanType, out var parsedPlanType))
        {
            planType = parsedPlanType;
        }

        // Update Stripe product
        var productService = new ProductService();
        await productService.UpdateAsync(plan.StripeProductId, new ProductUpdateOptions
        {
            Name = dto.Name ?? plan.Name,
            Description = dto.Description ?? plan.Description,
            Active = dto.IsActive
        });

        // If price changed, create a new price in Stripe
        if (dto.Price != plan.Price || dto.Interval != plan.Interval)
        {
            var priceOptions = new PriceCreateOptions
            {
                Product = plan.StripeProductId,
                UnitAmount = (long)((dto.Price > 0 ? dto.Price : plan.Price) * 100), // Convert to cents
                Currency = "usd",
                Recurring = new PriceRecurringOptions
                {
                    Interval = dto.Interval ?? plan.Interval
                }
            };

            var priceService = new PriceService();
            var price = await priceService.CreateAsync(priceOptions);

            plan.StripePriceId = price.Id;
        }

        // Update entity
        plan.Name = dto.Name ?? plan.Name;
        plan.Description = dto.Description ?? plan.Description;
        if (dto.Price > 0) plan.Price = dto.Price;
        if (!string.IsNullOrEmpty(dto.Interval)) plan.Interval = dto.Interval;
        if (planType.HasValue) plan.PlanType = planType.Value;
        plan.IsActive = dto.IsActive;
        plan.ModifiedDate = DateTime.UtcNow;

        // Update features if provided
        if (dto.Features != null && dto.Features.Any())
        {
            // Remove existing features
            _dbContext.Set<SubscriptionPlanFeature>().RemoveRange(plan.Features);

            // Add new features
            plan.Features = dto.Features.Select(f => new SubscriptionPlanFeature
            {
                SubscriptionPlanId = plan.Id,
                Description = f
            }).ToList();
        }

        await _dbContext.SaveChangesAsync();

        return MapToDTO(plan);
    }

    public async Task<bool> DeletePlanAsync(Guid id)
    {
        var plan = await _dbContext.Set<SubscriptionPlanEntity>()
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            throw new KeyNotFoundException($"Subscription plan with ID {id} not found");
        }

        // Check if any subscriptions are using this plan
        var hasActiveSubscriptions = await _dbContext.Set<DomainSubscription>()
            .AnyAsync(s => s.Plan == plan.PlanType && s.IsActive);

        if (hasActiveSubscriptions)
        {
            throw new InvalidOperationException("Cannot delete a plan that has active subscriptions");
        }

        // Delete from Stripe
        var productService = new ProductService();
        await productService.UpdateAsync(plan.StripeProductId, new ProductUpdateOptions
        {
            Active = false
        });

        // Delete from database
        _dbContext.Set<SubscriptionPlanEntity>().Remove(plan);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ActivatePlanAsync(Guid id)
    {
        var plan = await _dbContext.Set<SubscriptionPlanEntity>()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            throw new KeyNotFoundException($"Subscription plan with ID {id} not found");
        }

        if (plan.IsActive)
        {
            return true; // Already active
        }

        // Activate in Stripe
        var productService = new ProductService();
        await productService.UpdateAsync(plan.StripeProductId, new ProductUpdateOptions
        {
            Active = true
        });

        // Update entity
        plan.IsActive = true;
        plan.ModifiedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivatePlanAsync(Guid id)
    {
        var plan = await _dbContext.Set<SubscriptionPlanEntity>()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            throw new KeyNotFoundException($"Subscription plan with ID {id} not found");
        }

        if (!plan.IsActive)
        {
            return true; // Already inactive
        }

        // Check if any subscriptions are using this plan
        var hasActiveSubscriptions = await _dbContext.Set<DomainSubscription>()
            .AnyAsync(s => s.Plan == plan.PlanType && s.IsActive);

        if (hasActiveSubscriptions)
        {
            throw new InvalidOperationException("Cannot deactivate a plan that has active subscriptions");
        }

        // Deactivate in Stripe
        var productService = new ProductService();
        await productService.UpdateAsync(plan.StripeProductId, new ProductUpdateOptions
        {
            Active = false
        });

        // Update entity
        plan.IsActive = false;
        plan.ModifiedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    private SubscriptionPlanDTO MapToDTO(SubscriptionPlanEntity plan)
    {
        return new SubscriptionPlanDTO
        {
            Id = plan.Id,
            PlanId = plan.PlanId,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            Interval = plan.Interval,
            PlanType = plan.PlanType.ToString(),
            IsActive = plan.IsActive,
            Features = plan.Features.Select(f => f.Description).ToList()
        };
    }
} 