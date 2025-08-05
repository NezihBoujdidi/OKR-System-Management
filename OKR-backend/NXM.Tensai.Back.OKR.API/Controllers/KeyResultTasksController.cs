using NXM.Tensai.Back.OKR.Application.Features.KeyResultTasks.Commands;
namespace NXM.Tensai.Back.OKR.API;

[Route("api/keyresulttasks")]
[ApiController]
public class KeyResultTasksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<KeyResultTasksController> _logger;

    public KeyResultTasksController(IMediator mediator, ILogger<KeyResultTasksController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Authorize(Policy = Permissions.KeyResultTasks_Create)]
    [HttpPost]
    public async Task<IActionResult> CreateKeyResultTask([FromBody] CreateKeyResultTaskCommand command)
    {
        _logger.LogInformation("CreateKeyResultTask attempt for key result task title: {KeyResultTaskTitle}", command.Title);

        try
        {
            await _mediator.Send(command);
            _logger.LogInformation("CreateKeyResultTask successful for key result task title: {KeyResultTaskTitle}", command.Title);
            return Ok("Key result task created successfully.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for key result task title: {KeyResultTaskTitle} - {Errors}", command.Title, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during CreateKeyResultTask attempt for key result task title: {KeyResultTaskTitle}", command.Title);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.KeyResultTasks_Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateKeyResultTask(Guid id, [FromBody] UpdateKeyResultTaskCommand command)
    {
        _logger.LogInformation("UpdateKeyResultTask attempt for key result task ID: {KeyResultTaskId}", id);

        try
        {
            var updateCommand = new UpdateKeyResultTaskCommandWithId(id, command);
            await _mediator.Send(updateCommand);
            _logger.LogInformation("UpdateKeyResultTask successful for key result task ID: {KeyResultTaskId}", id);
            return Ok("Key result task updated successfully.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for key result task ID: {KeyResultTaskId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Key result task not found: {KeyResultTaskId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during UpdateKeyResultTask attempt for key result task ID: {KeyResultTaskId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.KeyResultTasks_Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteKeyResultTask(Guid id)
    {
        _logger.LogInformation("DeleteKeyResultTask attempt for key result task ID: {KeyResultTaskId}", id);

        try
        {
            var command = new DeleteKeyResultTaskCommand(id);
            await _mediator.Send(command);
            _logger.LogInformation("DeleteKeyResultTask successful for key result task ID: {KeyResultTaskId}", id);
            return Ok("Key result task deleted successfully.");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Key result task not found: {KeyResultTaskId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during DeleteKeyResultTask attempt for key result task ID: {KeyResultTaskId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.KeyResultTasks_GetAll)]
    [HttpGet]
    public async Task<IActionResult> GetAllKeyResultTasks([FromQuery] SearchKeyResultTasksQuery query)
    {
        _logger.LogInformation("GetAllKeyResultTasks attempt");

        try
        {
            var keyResultTasks = await _mediator.Send(query);
            _logger.LogInformation("GetAllKeyResultTasks successful");
            return Ok(keyResultTasks);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for GetAllKeyResultTasks: {Errors}", ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetAllKeyResultTasks attempt");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.KeyResultTasks_GetById)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetKeyResultTaskById(Guid id)
    {
        _logger.LogInformation("GetKeyResultTaskById attempt for key result task ID: {KeyResultTaskId}", id);

        try
        {
            var query = new GetKeyResultTaskByIdQuery(id);
            var keyResultTask = await _mediator.Send(query);
            _logger.LogInformation("GetKeyResultTaskById successful for key result task ID: {KeyResultTaskId}", id);
            return Ok(keyResultTask);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for key result task ID: {KeyResultTaskId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Key result task not found: {KeyResultTaskId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetKeyResultTaskById attempt for key result task ID: {KeyResultTaskId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
     [Authorize(Policy = Permissions.KeyResultTasks_GetAll)]
    [HttpGet("keyresult/{keyresultId:guid}")]
    public async Task<IActionResult> GetKeyResultTasksByKeyResultId(Guid keyresultId)
    {
        _logger.LogInformation("GetKeyResultTasksByKeyResultId attempt for objective ID: {keyresultId}", keyresultId);

        try
        {
            var query = new GetKeyResultsTasksByKeyResultIdQuery(keyresultId);
            var keyresults = await _mediator.Send(query);
            _logger.LogInformation("GetKeyResultTasksByKeyResultId successful for objective ID: {keyresultId}", keyresultId);
            return Ok(keyresults);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for objective ID: {keyresultId} - {Errors}", keyresultId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "No keyresults found for objective ID: {keyresultId}", keyresultId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetKeyResultTasksByKeyResultId attempt for objective ID: {keyresultId}", keyresultId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPatch("toggle-status")]
    public async Task<IActionResult> ToggleStatus([FromBody] ToggleKeyResultTaskStatusCommand command)
    {
        await _mediator.Send(command);
        return Ok();
    }
}
