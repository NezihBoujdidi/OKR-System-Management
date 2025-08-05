using ValidationException = NXM.Tensai.Back.OKR.Application.Common.Exceptions.ValidationException;

namespace NXM.Tensai.Back.OKR.API;

[Route("api/users")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Authorize(Policy = Permissions.Users_Create)]
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Create user attempt for FirstName: {FirstName}, LastName: {LastName}", command.FirstName, command.LastName);

        try
        {
            var createdUser = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("User creation successful for FirstName: {FirstName}, LastName: {LastName}", command.FirstName, command.LastName);
            return Ok(createdUser);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for user creation with FirstName: {FirstName}, LastName: {LastName}", command.FirstName, command.LastName);
            return BadRequest(ex.Errors);
        }
        catch (RoleAssignmentException ex)
        {
            _logger.LogError(ex, "Role assignment failed for email: {Email}", command.Email);
            return StatusCode(500, ex.Message);
        }
    }

    //[Authorize(Policy = Permissions.Users_Update)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Update user attempt for ID: {UserId}", id);

        try
        {
            var updateCommand = new UpdateUserCommandWithId(id, command);
            var updatedUser = await _mediator.Send(updateCommand, cancellationToken);
            _logger.LogInformation("User update successful for ID: {UserId}", id);
            return Ok(updatedUser);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for user update with ID: {UserId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found with ID: {UserId}", id);
            return NotFound(ex.Message);
        }
    }

    [Authorize(Policy = Permissions.Users_GetById)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get user by ID attempt for ID: {UserId}", id);

        try
        {
            var query = new GetUserByIdQuery { Id = id };
            var user = await _mediator.Send(query, cancellationToken);
            _logger.LogInformation("Get user by ID successful for ID: {UserId}", id);
            return Ok(user);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found with ID: {UserId}", id);
            return NotFound(ex.Message);
        }
    }

    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get user by email attempt for email: {Email}", email);

        try
        {
            var query = new GetUserByEmailQuery { Email = email };
            var user = await _mediator.Send(query, cancellationToken);
            _logger.LogInformation("Get user by email successful for email: {Email}", email);
            return Ok(user);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for email: {Email} - {Errors}", email, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found with email: {Email}", email);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while getting user by email: {Email}", email);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("supabase/{supabaseId}")]
    public async Task<IActionResult> GetUserBySupabaseId(string supabaseId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get user by Supabase ID attempt for ID: {SupabaseId}", supabaseId);

        try
        {
            var query = new GetUserBySupabaseIdQuery { SupabaseId = supabaseId };
            var user = await _mediator.Send(query, cancellationToken);
            _logger.LogInformation("Get user by Supabase ID successful for ID: {SupabaseId}", supabaseId);
            return Ok(user);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for Supabase ID: {SupabaseId} - {Errors}", supabaseId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found with Supabase ID: {SupabaseId}", supabaseId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while getting user by Supabase ID: {SupabaseId}", supabaseId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Users_Update)]
    [HttpPut("{id:guid}/disable")]
    public async Task<IActionResult> DisableUserById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Disable user attempt for ID: {UserId}", id);

        try
        {
            var command = new DisableUserByIdCommand(id);
            var disabledUser = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("User disabled successfully for ID: {UserId}", id);
            return Ok(disabledUser);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for disabling user with ID: {UserId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found with ID: {UserId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while disabling user with ID: {UserId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Users_Update)]
    [HttpPut("{id:guid}/enable")]
    public async Task<IActionResult> EnableUserById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Enable user attempt for ID: {UserId}", id);

        try
        {
            var command = new EnableUserByIdCommand(id);
            var enabledUser = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("User enabled successfully for ID: {UserId}", id);
            return Ok(enabledUser);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for enabling user with ID: {UserId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found with ID: {UserId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while enabling user with ID: {UserId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    //[Authorize(Policy = Permissions.Users_GetByOrganizationId)]
    [HttpGet("organization/{organizationId:guid}")]
    public async Task<IActionResult> GetUsersByOrganizationId(Guid organizationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get users by Organization ID attempt for ID: {OrganizationId}", organizationId);

        try
        {
            var query = new GetUsersByOrganizationIdQuery { OrganizationId = organizationId };
            var users = await _mediator.Send(query, cancellationToken);
            _logger.LogInformation("Get users by organization ID successful for organization ID: {OrganizationId}", organizationId);
            return Ok(users);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for organization ID: {OrganizationId} - {Errors}", organizationId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "No users found for organization ID: {OrganizationId}", organizationId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while getting users for organization ID: {OrganizationId}", organizationId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    //[Authorize(Policy = Permissions.Users_GetByOrganizationId)]
    [HttpGet("organization/{organizationId:guid}/teammanagers")]
    public async Task<IActionResult> GetTeamManagersByOrganizationId(Guid organizationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get team managers by Organization ID attempt for ID: {OrganizationId}", organizationId);

        try
        {
            var query = new GetTeamManagersByOrganizationIdQuery { OrganizationId = organizationId };
            var teamManagers = await _mediator.Send(query, cancellationToken);
            _logger.LogInformation("Get team managers by organization ID successful for organization ID: {OrganizationId}", organizationId);
            return Ok(teamManagers);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for organization ID: {OrganizationId} - {Errors}", organizationId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "No team managers found for organization ID: {OrganizationId}", organizationId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while getting team managers for organization ID: {OrganizationId}", organizationId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Users_GetAll)]
    [HttpGet]
    public async Task<IActionResult> GetAllUsers([FromQuery] GetAllUsersQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllUsers attempt");

        try
        {
            var users = await _mediator.Send(query, cancellationToken);
            _logger.LogInformation("GetAllUsers successful");
            return Ok(users);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for GetAllUsers: {Errors}", ex.Errors);
            return BadRequest(ex.Errors);
        }
    }

    [Authorize(Policy = Permissions.Users_Invite)]
    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser(InviteUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invitation attempt for user with email: {Email}", command.Email);

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            if (result.Success)
            {
                _logger.LogInformation("Invitation sent successfully to {Email}", command.Email);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Invitation failed for {Email}: {Message}", command.Email, result.Message);
                return StatusCode(500, result);
            }
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for invitation to {Email}", command.Email);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "Entity not found when inviting {Email}", command.Email);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while inviting user {Email}", command.Email);
            return StatusCode(500, "An unexpected error occurred while sending the invitation.");
        }
    }

    [HttpGet("organization/{organizationId:guid}/collaborators")]
    public async Task<IActionResult> GetCollaboratorsByOrganizationId(Guid organizationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get collaborators by Organization ID attempt for ID: {OrganizationId}", organizationId);

        try
        {
            var query = new GetCollaboratorsByOrganizationIdQuery { OrganizationId = organizationId };
            var collaborators = await _mediator.Send(query, cancellationToken);
            _logger.LogInformation("Get collaborators by organization ID successful for organization ID: {OrganizationId}", organizationId);
            return Ok(collaborators);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for organization ID: {OrganizationId} - {Errors}", organizationId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "No collaborators found for organization ID: {OrganizationId}", organizationId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while getting collaborators for organization ID: {OrganizationId}", organizationId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("organization/{organizationId:guid}/admin/email")]
    public async Task<IActionResult> GetOrganizationAdminEmail(Guid organizationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get organization admin email attempt for Organization ID: {OrganizationId}", organizationId);

        try
        {
            var query = new GetOrganizationAdminEmailQuery { OrganizationId = organizationId };
            var adminEmail = await _mediator.Send(query, cancellationToken);
            _logger.LogInformation("Get organization admin email successful for Organization ID: {OrganizationId}", organizationId);
            return Ok(adminEmail);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for organization ID: {OrganizationId} - {Errors}", organizationId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "No admin found for organization ID: {OrganizationId}", organizationId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while getting admin email for organization ID: {OrganizationId}", organizationId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
