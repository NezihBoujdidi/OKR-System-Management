namespace NXM.Tensai.Back.OKR.API;

[Route("api/objectives")]
[ApiController]
public class ObjectivesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ObjectivesController> _logger;

    public ObjectivesController(IMediator mediator, ILogger<ObjectivesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Authorize(Policy = Permissions.Objectives_Create)]
    [HttpPost]
    public async Task<IActionResult> CreateObjective([FromBody] CreateObjectiveCommand command)
    {
        _logger.LogInformation("CreateObjective attempt for objective title: {ObjectiveTitle}", command.Title);

        try
        {
            await _mediator.Send(command);
            _logger.LogInformation("CreateObjective successful for objective title: {ObjectiveTitle}", command.Title);
            return Ok("Objective created successfully.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for objective title: {ObjectiveTitle} - {Errors}", command.Title, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during CreateObjective attempt for objective title: {ObjectiveTitle}", command.Title);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Objectives_Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateObjective(Guid id, [FromBody] UpdateObjectiveCommand command)
    {
        _logger.LogInformation("UpdateObjective attempt for objective ID: {ObjectiveId}", id);

        try
        {
            var updateCommand = new UpdateObjectiveCommandWithId(id, command);
            await _mediator.Send(updateCommand);
            _logger.LogInformation("UpdateObjective successful for objective ID: {ObjectiveId}", id);
            return Ok("Objective updated successfully.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for objective ID: {ObjectiveId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Objective not found: {ObjectiveId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during UpdateObjective attempt for objective ID: {ObjectiveId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Objectives_Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteObjective(Guid id)
    {
        _logger.LogInformation("DeleteObjective attempt for objective ID: {ObjectiveId}", id);

        try
        {
            var command = new DeleteObjectiveCommand(id);
            await _mediator.Send(command);
            _logger.LogInformation("DeleteObjective successful for objective ID: {ObjectiveId}", id);
            return Ok("Objective deleted successfully.");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Objective not found: {ObjectiveId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during DeleteObjective attempt for objective ID: {ObjectiveId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Objectives_GetAll)]
    [HttpGet]
    public async Task<IActionResult> GetAllObjectives([FromQuery] SearchObjectivesQuery query)
    {
        _logger.LogInformation("GetAllObjectives attempt");

        try
        {
            var objectives = await _mediator.Send(query);
            _logger.LogInformation("GetAllObjectives successful");
            return Ok(objectives);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for GetAllObjectives: {Errors}", ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetAllObjectives attempt");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Objectives_GetById)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetObjectiveById(Guid id)
    {
        _logger.LogInformation("GetObjectiveById attempt for objective ID: {ObjectiveId}", id);

        try
        {
            var query = new GetObjectiveByIdQuery(id);
            var objective = await _mediator.Send(query);
            _logger.LogInformation("GetObjectiveById successful for objective ID: {ObjectiveId}", id);
            return Ok(objective);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for objective ID: {ObjectiveId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Objective not found: {ObjectiveId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetObjectiveById attempt for objective ID: {ObjectiveId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.Objectives_GetAll)]
    [HttpGet("session/{okrSessionId:guid}")]
    public async Task<IActionResult> GetObjectivesBySessionId(Guid okrSessionId)
    {
        _logger.LogInformation("GetObjectivesBySessionId attempt for session ID: {SessionId}", okrSessionId);

        try
        {
            var query = new GetObjectivesBySessionIdQuery(okrSessionId);
            var objectives = await _mediator.Send(query);
            _logger.LogInformation("GetObjectivesBySessionId successful for session ID: {okrSessionId}", okrSessionId);
            return Ok(objectives);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for session ID: {okrSessionId} - {Errors}", okrSessionId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "No objectives found for session ID: {okrSessionId}", okrSessionId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetObjectivesBySessionId attempt for session ID: {okrSessionId}", okrSessionId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
