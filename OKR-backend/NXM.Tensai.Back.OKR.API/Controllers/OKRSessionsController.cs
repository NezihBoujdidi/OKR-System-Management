namespace NXM.Tensai.Back.OKR.API;

[Route("api/okrsessions")]
[ApiController]
public class OKRSessionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OKRSessionsController> _logger;

    public OKRSessionsController(IMediator mediator, ILogger<OKRSessionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Authorize(Policy = Permissions.OKRSessions_Create)]
    [HttpPost]
    public async Task<IActionResult> CreateOKRSession([FromBody] CreateOKRSessionCommand command)
    {
        _logger.LogInformation("CreateOKRSession attempt for OKR session title: {OKRSessionTitle}", command.Title);

        try
        {
            var sessionId = await _mediator.Send(command);
            var query = new GetOKRSessionByIdQuery(sessionId);
            var createdSession = await _mediator.Send(query);
            
            _logger.LogInformation("CreateOKRSession successful for OKR session title: {OKRSessionTitle}", command.Title);
            return CreatedAtAction(nameof(GetOKRSessionById), new { id = sessionId }, createdSession);
        }
        catch (ValidationException ex) when (ex.Message.Contains("Team with ID"))
        {
            _logger.LogWarning(ex, "Team validation failed for OKR session title: {OKRSessionTitle}", command.Title);
            return BadRequest(new { Error = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for OKR session title: {OKRSessionTitle} - {Errors}", command.Title, ex.Errors);
            return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during CreateOKRSession attempt for OKR session title: {OKRSessionTitle}", command.Title);
            return StatusCode(500, new { Error = "An unexpected error occurred while creating the OKR session." });
        }
    }

    [Authorize(Policy = Permissions.OKRSessions_Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateOKRSession(Guid id, [FromBody] UpdateOKRSessionCommand command)
    {
        _logger.LogInformation("UpdateOKRSession attempt for OKR session ID: {OKRSessionId}", id);

        try
        {
            var updateCommand = new UpdateOKRSessionCommandWithId(id, command);
            await _mediator.Send(updateCommand);
            _logger.LogInformation("UpdateOKRSession successful for OKR session ID: {OKRSessionId}", id);
            return Ok("OKR session updated successfully.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for OKR session ID: {OKRSessionId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "OKR session not found: {OKRSessionId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during UpdateOKRSession attempt for OKR session ID: {OKRSessionId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.OKRSessions_Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOKRSession(Guid id)
    {
        _logger.LogInformation("DeleteOKRSession attempt for OKR session ID: {OKRSessionId}", id);

        try
        {
            var command = new DeleteOKRSessionCommand(id);
            await _mediator.Send(command);
            _logger.LogInformation("DeleteOKRSession successful for OKR session ID: {OKRSessionId}", id);
            return Ok("OKR session deleted successfully.");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "OKR session not found: {OKRSessionId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during DeleteOKRSession attempt for OKR session ID: {OKRSessionId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.OKRSessions_GetAll)]
    [HttpGet]
    public async Task<IActionResult> GetAllOKRSessions([FromQuery] SearchOKRSessionsQuery query)
    {
        _logger.LogInformation("GetAllOKRSessions attempt");

        try
        {
            var okrSessions = await _mediator.Send(query);
            _logger.LogInformation("GetAllOKRSessions successful");
            return Ok(okrSessions);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for GetAllOKRSessions: {Errors}", ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetAllOKRSessions attempt");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.OKRSessions_GetAll)]
    [HttpGet("organization/{organizationId:guid}")]
    public async Task<IActionResult> GetOKRSessionsByOrganizationId(Guid organizationId)
    {
        _logger.LogInformation("GetOKRSessionsByOrganizationId attempt for organization ID: {OrganizationId}", organizationId);

        try
        {
            var query = new SearchOKRSessionsByOrganizationIdQuery(organizationId);
            var okrSessions = await _mediator.Send(query);
            _logger.LogInformation("GetOKRSessionsByOrganizationId successful for organization ID: {OrganizationId}", organizationId);
            return Ok(okrSessions);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for organization ID: {OrganizationId} - {Errors}", organizationId, ex.Errors);
            return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetOKRSessionsByOrganizationId attempt for organization ID: {OrganizationId}", organizationId);
            return StatusCode(500, new { Error = "An unexpected error occurred while retrieving the OKR sessions." });
        }
    }

    [Authorize(Policy = Permissions.OKRSessions_GetById)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOKRSessionById(Guid id)
    {
        _logger.LogInformation("GetOKRSessionById attempt for OKR session ID: {OKRSessionId}", id);

        try
        {
            var query = new GetOKRSessionByIdQuery(id);
            var okrSession = await _mediator.Send(query);
            _logger.LogInformation("GetOKRSessionById successful for OKR session ID: {OKRSessionId}", id);
            return Ok(okrSession);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for OKR session ID: {OKRSessionId} - {Errors}", id, ex.Errors);
            return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "OKR session not found with ID: {OKRSessionId}", id);
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetOKRSessionById attempt for ID: {OKRSessionId}", id);
            return StatusCode(500, new { Error = "An unexpected error occurred while retrieving the OKR session." });
        }
    }
    [Authorize(Policy = Permissions.OKRSessions_GetAll)]
    [HttpGet("by-teamId/{teamId}")]
    public async Task<IActionResult> GetOKRSessionsByTeam(Guid teamId)
    {
        var result = await _mediator.Send(new GetOKRSessionsByTeamIdQuery(teamId));
        return Ok(result);
    }
}
