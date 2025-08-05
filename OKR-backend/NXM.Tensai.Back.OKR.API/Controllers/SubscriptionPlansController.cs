using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;

namespace NXM.Tensai.Back.OKR.API.Controllers;

[Route("api/subscription-plans")]
[ApiController]
// [Authorize(Policy = "SuperAdmin")] // Ensure only super admins can manage plans
public class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _subscriptionPlanService;
    private readonly ILogger<SubscriptionPlansController> _logger;

    public SubscriptionPlansController(
        ISubscriptionPlanService subscriptionPlanService,
        ILogger<SubscriptionPlansController> logger)
    {
        _subscriptionPlanService = subscriptionPlanService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPlans([FromQuery] bool includeInactive = false)
    {
        try
        {
            var plans = await _subscriptionPlanService.GetAllPlansAsync(includeInactive);
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plans");
            return StatusCode(500, "An error occurred while retrieving the subscription plans");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlanById(Guid id)
    {
        try
        {
            var plan = await _subscriptionPlanService.GetPlanByIdAsync(id);
            return Ok(plan);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plan {PlanId}", id);
            return StatusCode(500, "An error occurred while retrieving the subscription plan");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanDTO dto)
    {
        try
        {
            var plan = await _subscriptionPlanService.CreatePlanAsync(dto);
            return CreatedAtAction(nameof(GetPlanById), new { id = plan.Id }, plan);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription plan");
            return StatusCode(500, "An error occurred while creating the subscription plan");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateSubscriptionPlanDTO dto)
    {
        try
        {
            var plan = await _subscriptionPlanService.UpdatePlanAsync(id, dto);
            return Ok(plan);
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
            _logger.LogError(ex, "Error updating subscription plan {PlanId}", id);
            return StatusCode(500, "An error occurred while updating the subscription plan");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        try
        {
            await _subscriptionPlanService.DeletePlanAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription plan {PlanId}", id);
            return StatusCode(500, "An error occurred while deleting the subscription plan");
        }
    }

    [HttpPut("{id}/activate")]
    public async Task<IActionResult> ActivatePlan(Guid id)
    {
        try
        {
            await _subscriptionPlanService.ActivatePlanAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating subscription plan {PlanId}", id);
            return StatusCode(500, "An error occurred while activating the subscription plan");
        }
    }

    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> DeactivatePlan(Guid id)
    {
        try
        {
            await _subscriptionPlanService.DeactivatePlanAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating subscription plan {PlanId}", id);
            return StatusCode(500, "An error occurred while deactivating the subscription plan");
        }
    }
} 