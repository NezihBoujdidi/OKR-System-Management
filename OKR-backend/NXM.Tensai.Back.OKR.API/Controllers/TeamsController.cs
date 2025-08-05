using ValidationException = NXM.Tensai.Back.OKR.Application.Common.Exceptions.ValidationException;

namespace NXM.Tensai.Back.OKR.API;

[Route("api/teams")]
[ApiController]
public class TeamsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(IMediator mediator, ILogger<TeamsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    //[Authorize(Policy = Permissions.Teams_Create)]
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamCommand command)
    {
        _logger.LogInformation("CreateTeam attempt for team name: {TeamName}", command.Name);

        try
        {
            var teamId = await _mediator.Send(command);
            _logger.LogInformation("CreateTeam successful for team name: {TeamName} with ID: {TeamId}", command.Name, teamId);
            return Ok(teamId);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for team name: {TeamName} - {Errors}", command.Name, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during CreateTeam attempt for team name: {TeamName}", command.Name);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    //[Authorize(Policy = Permissions.Teams_Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTeam(Guid id, [FromBody] UpdateTeamCommand command)
    {
        _logger.LogInformation("UpdateTeam attempt for team ID: {TeamId}", id);

        try
        {
            var updateCommand = new UpdateTeamCommandWithId(id, command);
            await _mediator.Send(updateCommand);
            _logger.LogInformation("UpdateTeam successful for team ID: {TeamId}", id);
            return Ok("Team updated successfully.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for team ID: {TeamId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Team not found: {TeamId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during UpdateTeam attempt for team ID: {TeamId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    //[Authorize(Policy = Permissions.Teams_Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTeam(Guid id)
    {
        _logger.LogInformation("DeleteTeam attempt for team ID: {TeamId}", id);

        try
        {
            var command = new DeleteTeamCommand(id);
            await _mediator.Send(command);
            _logger.LogInformation("DeleteTeam successful for team ID: {TeamId}", id);
            return Ok("Team deleted successfully.");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Team not found: {TeamId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during DeleteTeam attempt for team ID: {TeamId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    //[Authorize(Policy = Permissions.Teams_GetAll)]
    [HttpGet]
    public async Task<IActionResult> GetAllTeams([FromQuery] SearchTeamsQuery query)
    {
        _logger.LogInformation("GetAllTeams attempt");

        try
        {
            var teams = await _mediator.Send(query);
            _logger.LogInformation("GetAllTeams successful");
            return Ok(teams);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for GetAllTeams: {Errors}", ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetAllTeams attempt");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    //[Authorize(Policy = Permissions.Teams_GetById)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTeamById(Guid id)
    {
        _logger.LogInformation("GetTeamById attempt for team ID: {TeamId}", id);

        try
        {
            var query = new GetTeamByIdQuery(id);
            var team = await _mediator.Send(query);
            _logger.LogInformation("GetTeamById successful for team ID: {TeamId}", id);
            return Ok(team);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for team ID: {TeamId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Team not found: {TeamId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetTeamById attempt for team ID: {TeamId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    //[Authorize(Policy = Permissions.Teams_GetByManagerId)]
    [HttpGet("manager/{managerId:guid}")]
    public async Task<IActionResult> GetTeamsByManagerId(Guid managerId)
    {
        _logger.LogInformation("GetTeamsByManagerId attempt for manager ID: {ManagerId}", managerId);

        try
        {
            var query = new GetTeamsByManagerIdQuery { ManagerId = managerId };
            var teams = await _mediator.Send(query);
            _logger.LogInformation("GetTeamsByManagerId successful for manager ID: {ManagerId}", managerId);
            return Ok(teams);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for manager ID: {ManagerId} - {Errors}", managerId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "No teams found for manager ID: {ManagerId}", managerId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetTeamsByManagerId attempt for manager ID: {ManagerId}", managerId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    //[Authorize(Policy = Permissions.Teams_GetByCollaboratorId)]
    [HttpGet("collaborator/{collaboratorId:guid}")]
    public async Task<IActionResult> GetTeamsByCollaboratorId(Guid collaboratorId)
    {
        _logger.LogInformation("GetTeamsByCollaboratorId attempt for collaborator ID: {CollaboratorId}", collaboratorId);

        try
        {
            var query = new GetTeamsByCollaboratorIdQuery { CollaboratorId = collaboratorId };
            var teams = await _mediator.Send(query);
            _logger.LogInformation("GetTeamsByCollaboratorId successful for collaborator ID: {CollaboratorId}", collaboratorId);
            return Ok(teams);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for collaborator ID: {CollaboratorId} - {Errors}", collaboratorId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "No teams found for collaborator ID: {CollaboratorId}", collaboratorId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetTeamsByCollaboratorId attempt for collaborator ID: {CollaboratorId}", collaboratorId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
    
    //[Authorize(Policy = Permissions.Teams_GetByOrganizationId)]
    [HttpGet("organization/{organizationId:guid}")]
    public async Task<IActionResult> GetTeamsByOrganizationId(Guid organizationId)
    {
        _logger.LogInformation("GetTeamsByOrganizationId attempt for organization ID: {OrganizationId}", organizationId);

        try
        {
            var query = new GetTeamsByOrganizationIdQuery { OrganizationId = organizationId };
            var teams = await _mediator.Send(query);
            _logger.LogInformation("GetTeamsByOrganizationId successful for organization ID: {OrganizationId}", organizationId);
            return Ok(teams);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for organization ID: {OrganizationId} - {Errors}", organizationId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "No teams found for organization ID: {OrganizationId}", organizationId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetTeamsByOrganizationId attempt for organization ID: {OrganizationId}", organizationId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
    [HttpGet("by-user/{userId}")]
    public async Task<IActionResult> GetTeamsByUserId(Guid userId)
    {
        var result = await _mediator.Send(new GetTeamsByUserIdQuery(userId));
        return Ok(result);
    }
      [HttpGet("manager/details/{teamId}")]
    public async Task<IActionResult> GetTeamManagerByTeamId(Guid teamId)
    {  
        _logger.LogInformation("GetTeamManagerByTeamId attempt for team ID: {TeamId}", teamId);
        var query = new GetTeamManagerByTeamIdQuery{Id = teamId};
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetTeamsBySessionId(Guid sessionId)
    {
        var result = await _mediator.Send(new GetTeamsByOKRSessionIdQuery { OKRSessionId = sessionId });
        return Ok(result);
    }
}

