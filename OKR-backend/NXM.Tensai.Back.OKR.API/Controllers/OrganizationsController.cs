using ValidationException = NXM.Tensai.Back.OKR.Application.Common.Exceptions.ValidationException;

namespace NXM.Tensai.Back.OKR.API;

[Route("api/organizations")]
[ApiController]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrganizationsController> _logger;

    public OrganizationsController(IMediator mediator, ILogger<OrganizationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    //[Authorize(Policy = Permissions.Organizations_Create)]
    [HttpPost]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationCommand command)
    {
        _logger.LogInformation("CreateOrganization attempt for organization name: {OrganizationName}", command.Name);

        try
        {
            var organizationId = await _mediator.Send(command);
            _logger.LogInformation("CreateOrganization successful for organization name: {OrganizationName}", command.Name);
            return Ok(new { Id = organizationId, Message = "Organization created successfully." });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for organization name: {OrganizationName} - {Errors}", command.Name, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during CreateOrganization attempt for organization name: {OrganizationName}", command.Name);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Organizations_Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateOrganization(Guid id, [FromBody] UpdateOrganizationCommand command)
    {
        _logger.LogInformation("UpdateOrganization attempt for organization ID: {OrganizationId}", id);

        try
        {
            var updateCommand = new UpdateOrganizationCommandWithId(id, command);
            await _mediator.Send(updateCommand);
            _logger.LogInformation("UpdateOrganization successful for organization ID: {OrganizationId}", id);
            return Ok("Organization updated successfully.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for organization ID: {OrganizationId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Organization not found: {OrganizationId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during UpdateOrganization attempt for organization ID: {OrganizationId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Organizations_Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOrganization(Guid id)
    {
        _logger.LogInformation("DeleteOrganization attempt for organization ID: {OrganizationId}", id);

        try
        {
            var command = new DeleteOrganizationCommand(id);
            await _mediator.Send(command);
            _logger.LogInformation("DeleteOrganization successful for organization ID: {OrganizationId}", id);
            return Ok("Organization deleted successfully.");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Organization not found: {OrganizationId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during DeleteOrganization attempt for organization ID: {OrganizationId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Organizations_GetAll)]
    [HttpGet]
    public async Task<IActionResult> GetAllOrganizations([FromQuery] SearchOrganizationsQuery query)
    {
        _logger.LogInformation("GetAllOrganizations attempt");

        try
        {
            var organizations = await _mediator.Send(query);
            _logger.LogInformation("GetAllOrganizations successful");
            return Ok(organizations);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for GetAllOrganizations: {Errors}", ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetAllOrganizations attempt");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Organizations_GetById)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrganizationById(Guid id)
    {
        _logger.LogInformation("GetOrganizationById attempt for organization ID: {OrganizationId}", id);

        try
        {
            var query = new GetOrganizationByIdQuery(id);
            var organization = await _mediator.Send(query);
            _logger.LogInformation("GetOrganizationById successful for organization ID: {OrganizationId}", id);
            return Ok(organization);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for organization ID: {OrganizationId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Organization not found: {OrganizationId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetOrganizationById attempt for organization ID: {OrganizationId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
