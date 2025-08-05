namespace NXM.Tensai.Back.OKR.API;

[Route("api/roles")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly RolePermissionUpdateService _rolePermissionUpdateService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IMediator mediator, RolePermissionUpdateService rolePermissionUpdateService, ILogger<RolesController> logger)
    {
        _mediator = mediator;
        _rolePermissionUpdateService = rolePermissionUpdateService;
        _logger = logger;
    }

    [Authorize(Policy = Permissions.Roles_Create)]
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command)
    {
        _logger.LogInformation("CreateRole attempt for role name: {RoleName}", command.RoleName);

        try
        {
            await _mediator.Send(command);
            _logger.LogInformation("CreateRole successful for role name: {RoleName}", command.RoleName);
            return Ok("Role created successfully.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation failed for role name: {RoleName}", command.RoleName);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Role already exists: {RoleName}", command.RoleName);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during CreateRole attempt for role name: {RoleName}", command.RoleName);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Roles_Update)]
    [HttpPut]
    public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleCommand command)
    {
        _logger.LogInformation("UpdateRole attempt for old role name: {OldRoleName} to new role name: {NewRoleName}", command.OldRoleName, command.NewRoleName);

        try
        {
            await _mediator.Send(command);
            _logger.LogInformation("UpdateRole successful for old role name: {OldRoleName} to new role name: {NewRoleName}", command.OldRoleName, command.NewRoleName);
            return Ok("Role updated successfully.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for role update from {OldRoleName} to {NewRoleName} - {Errors}", command.OldRoleName, command.NewRoleName, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Role not found: {OldRoleName}", command.OldRoleName);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during UpdateRole attempt for old role name: {OldRoleName} to new role name: {NewRoleName}", command.OldRoleName, command.NewRoleName);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Roles_Delete)]
    [HttpDelete("{roleName}")]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        _logger.LogInformation("DeleteRole attempt for role name: {RoleName}", roleName);

        try
        {
            var command = new DeleteRoleCommand { RoleName = roleName };
            await _mediator.Send(command);
            _logger.LogInformation("DeleteRole successful for role name: {RoleName}", roleName);
            return Ok("Role deleted successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Role not found: {RoleName}", roleName);
            return NotFound(ex.Message);
        }
        catch (RoleHasUsersException ex)
        {
            _logger.LogWarning(ex, "Cannot delete role because it has users assigned to it: {RoleName}", roleName);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during DeleteRole attempt for role name: {RoleName}", roleName);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Roles_GetAll)]
    [HttpGet]
    public async Task<IActionResult> GetAllRoles([FromQuery] GetAllRolesQuery query)
    {
        _logger.LogInformation("GetAllRoles attempt");

        try
        {
            var roles = await _mediator.Send(query);
            _logger.LogInformation("GetAllRoles successful");
            return Ok(roles);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for GetAllRoles: {Errors}", ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetAllRoles attempt");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Roles_GetById)]
    [HttpGet("{roleName}")]
    public async Task<IActionResult> GetRoleByName(string roleName)
    {
        _logger.LogInformation("GetRoleByName attempt for role name: {RoleName}", roleName);

        try
        {
            var query = new GetRoleByNameQuery { RoleName = roleName };
            var role = await _mediator.Send(query);
            _logger.LogInformation("GetRoleByName successful for role name: {RoleName}", roleName);
            return Ok(role);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for role name: {RoleName} - {Errors}", roleName, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Role not found: {RoleName}", roleName);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetRoleByName attempt for role name: {RoleName}", roleName);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Roles_Update)]
    [HttpPost("permissions")]
    public async Task<IActionResult> AddPermissionToRole([FromBody] AddPermissionToRoleCommand command)
    {
        _logger.LogInformation("AddPermissionToRole attempt for role name: {RoleName} and permission: {Permission}", command.RoleName, command.Permission);

        try
        {
            await _mediator.Send(command);
            _logger.LogInformation("AddPermissionToRole successful for role name: {RoleName} and permission: {Permission}", command.RoleName, command.Permission);
            return Ok("Permission added successfully to role.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for role name: {RoleName} and permission: {Permission} - {Errors}", command.RoleName, command.Permission, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Role not found: {RoleName}", command.RoleName);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Permission already exists for role: {RoleName}", command.RoleName);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during AddPermissionToRole attempt for role name: {RoleName} and permission: {Permission}", command.RoleName, command.Permission);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Roles_Update)]
    [HttpDelete("permissions")]
    public async Task<IActionResult> DeletePermissionFromRole([FromBody] DeletePermissionFromRoleCommand command)
    {
        _logger.LogInformation("DeletePermissionFromRole attempt for role name: {RoleName} and permission: {Permission}", command.RoleName, command.Permission);

        try
        {
            await _mediator.Send(command);
            _logger.LogInformation("DeletePermissionFromRole successful for role name: {RoleName} and permission: {Permission}", command.RoleName, command.Permission);
            return Ok("Permission deleted successfully from role.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for role name: {RoleName} and permission: {Permission} - {Errors}", command.RoleName, command.Permission, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Role not found: {RoleName}", command.RoleName);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Permission does not exist for role: {RoleName}", command.RoleName);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during DeletePermissionFromRole attempt for role name: {RoleName} and permission: {Permission}", command.RoleName, command.Permission);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Roles_Update)]
    [HttpPost("refresh-permissions/{roleName}")]
    public async Task<IActionResult> RefreshRolePermissions(string roleName)
    {
        try
        {
            await _rolePermissionUpdateService.UpdateRolePermissions(roleName);
            return Ok($"Successfully refreshed permissions for role {roleName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing permissions for role {RoleName}", roleName);
            return StatusCode(500, "An error occurred while refreshing role permissions");
        }
    }
}
