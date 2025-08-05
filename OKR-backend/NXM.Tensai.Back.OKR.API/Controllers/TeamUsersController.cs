using ValidationException = NXM.Tensai.Back.OKR.Application.Common.Exceptions.ValidationException;

namespace NXM.Tensai.Back.OKR.API;

[Route("api/teamusers")]
[ApiController]
public class TeamUsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TeamUsersController> _logger;

    public TeamUsersController(IMediator mediator, ILogger<TeamUsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    //[Authorize(Policy = Permissions.TeamUsers_GetByTeamId)]
    [HttpGet("team/{teamId:guid}/users")]
    public async Task<IActionResult> GetUsersByTeamId(Guid teamId)
    {
        _logger.LogInformation("GetUsersByTeamId attempt for team ID: {TeamId}", teamId);

        try
        {
            var query = new GetUsersByTeamIdQuery { TeamId = teamId };
            var users = await _mediator.Send(query);
            _logger.LogInformation("GetUsersByTeamId successful for team ID: {TeamId}", teamId);
            return Ok(users);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for team ID: {TeamId} - {Errors}", teamId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "No users found for team ID: {TeamId}", teamId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetUsersByTeamId attempt for team ID: {TeamId}", teamId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpDelete("team/{teamId:guid}/user/{userId:guid}")]
    public async Task<IActionResult> RemoveUserFromTeam(Guid teamId, Guid userId)
    {
        _logger.LogInformation("RemoveUserFromTeam attempt for team ID: {TeamId}, user ID: {UserId}", teamId, userId);

        try
        {
            var command = new RemoveUserFromTeamCommand { TeamId = teamId, UserId = userId };
            await _mediator.Send(command);
            _logger.LogInformation("RemoveUserFromTeam successful for team ID: {TeamId}, user ID: {UserId}", teamId, userId);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for team ID: {TeamId}, user ID: {UserId} - {Errors}", teamId, userId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "User or team not found for team ID: {TeamId}, user ID: {UserId}", teamId, userId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during RemoveUserFromTeam attempt for team ID: {TeamId}, user ID: {UserId}", teamId, userId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost("move-member")]
    public async Task<IActionResult> MoveMemberFromTeamToTeam([FromBody] MoveMemberFromTeamToTeamRequest request)
    {
        _logger.LogInformation("MoveMemberFromTeamToTeam attempt for member ID: {MemberId}, from team ID: {SourceTeamId} to team ID: {NewTeamId}", request.MemberId, request.SourceTeamId, request.NewTeamId);

        try
        {
            var command = new MoveMemberFromTeamToTeamCommand
            {
                MemberId = request.MemberId,
                SourceTeamId = request.SourceTeamId,
                NewTeamId = request.NewTeamId
            };
            await _mediator.Send(command);
            _logger.LogInformation("MoveMemberFromTeamToTeam successful for member ID: {MemberId}", request.MemberId);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for move member: {Errors}", ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "Entity not found during move member: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
        catch (UserHasOngoingTaskException ex)
        {
            _logger.LogWarning(ex, "User has ongoing task: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during MoveMemberFromTeamToTeam attempt for member ID: {MemberId}", request.MemberId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost("add-users")]
    public async Task<IActionResult> AddUsersToTeam([FromBody] AddUsersToTeamRequest request)
    {
        _logger.LogInformation("AddUsersToTeam attempt for team ID: {TeamId} with users: {UserIds}", request.TeamId, string.Join(",", request.UserIds));

        try
        {
            var command = new AddUsersToTeamCommand
            {
                TeamId = request.TeamId,
                UserIds = request.UserIds
            };
            var result = await _mediator.Send(command);
            _logger.LogInformation("AddUsersToTeam successful for team ID: {TeamId}", request.TeamId);

            // Return message and details in response
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for AddUsersToTeam: {Errors}", ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "Entity not found during AddUsersToTeam: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during AddUsersToTeam attempt for team ID: {TeamId}", request.TeamId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    public class MoveMemberFromTeamToTeamRequest
    {
        public Guid MemberId { get; set; }
        public Guid SourceTeamId { get; set; }
        public Guid NewTeamId { get; set; }
    }

    public class AddUsersToTeamRequest
    {
        public Guid TeamId { get; set; }
        public List<Guid> UserIds { get; set; } = new();
    }
}