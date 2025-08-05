namespace NXM.Tensai.Back.OKR.API;

[Route("api/keyresults")]
[ApiController]
public class KeyResultsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<KeyResultsController> _logger;

    public KeyResultsController(IMediator mediator, ILogger<KeyResultsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Authorize(Policy = Permissions.KeyResults_Create)]
    [HttpPost]
    public async Task<IActionResult> CreateKeyResult([FromBody] CreateKeyResultCommand command)
    {
        _logger.LogInformation("CreateKeyResult attempt for key result title: {KeyResultTitle}", command.Title);

        try
        {
            await _mediator.Send(command);
            _logger.LogInformation("CreateKeyResult successful for key result title: {KeyResultTitle}", command.Title);
            return Ok("Key result created successfully.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for key result title: {KeyResultTitle} - {Errors}", command.Title, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during CreateKeyResult attempt for key result title: {KeyResultTitle}", command.Title);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.KeyResults_Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateKeyResult(Guid id, [FromBody] UpdateKeyResultCommand command)
    {
        _logger.LogInformation("UpdateKeyResult attempt for key result ID: {KeyResultId}", id);

        try
        {
            var updateCommand = new UpdateKeyResultCommandWithId(id, command);
            await _mediator.Send(updateCommand);
            _logger.LogInformation("UpdateKeyResult successful for key result ID: {KeyResultId}", id);
            return Ok("Key result updated successfully.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for key result ID: {KeyResultId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Key result not found: {KeyResultId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during UpdateKeyResult attempt for key result ID: {KeyResultId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.KeyResults_Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteKeyResult(Guid id)
    {
        _logger.LogInformation("DeleteKeyResult attempt for key result ID: {KeyResultId}", id);

        try
        {
            var command = new DeleteKeyResultCommand(id);
            await _mediator.Send(command);
            _logger.LogInformation("DeleteKeyResult successful for key result ID: {KeyResultId}", id);
            return Ok("Key result deleted successfully.");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Key result not found: {KeyResultId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during DeleteKeyResult attempt for key result ID: {KeyResultId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.KeyResults_GetAll)]
    [HttpGet]
    public async Task<IActionResult> GetAllKeyResults([FromQuery] SearchKeyResultsQuery query)
    {
        _logger.LogInformation("GetAllKeyResults attempt");

        try
        {
            var keyResults = await _mediator.Send(query);
            _logger.LogInformation("GetAllKeyResults successful");
            return Ok(keyResults);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for GetAllKeyResults: {Errors}", ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetAllKeyResults attempt");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Policy = Permissions.KeyResults_GetById)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetKeyResultById(Guid id)
    {
        _logger.LogInformation("GetKeyResultById attempt for key result ID: {KeyResultId}", id);

        try
        {
            var query = new GetKeyResultByIdQuery(id);
            var keyResult = await _mediator.Send(query);
            _logger.LogInformation("GetKeyResultById successful for key result ID: {KeyResultId}", id);
            return Ok(keyResult);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for key result ID: {KeyResultId} - {Errors}", id, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Key result not found: {KeyResultId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetKeyResultById attempt for key result ID: {KeyResultId}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
    
    [Authorize(Policy = Permissions.KeyResults_GetAll)]
    [HttpGet("objective/{objectiveId:guid}")]
    public async Task<IActionResult> GetKeyResultsByObjectiveId(Guid objectiveId)
    {
        _logger.LogInformation("GetKeyResultsByObjectiveId attempt for objective ID: {objectiveId}", objectiveId);

        try
        {
            var query = new GetKeyResultsByObjectiveIdQuery(objectiveId);
            var keyresults = await _mediator.Send(query);
            _logger.LogInformation("GetKeyResultsByObjectiveId successful for objective ID: {objectiveId}", objectiveId);
            return Ok(keyresults);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for objective ID: {objectiveId} - {Errors}", objectiveId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "No keyresults found for objective ID: {objectiveId}", objectiveId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GetKeyResultsByObjectiveId attempt for objective ID: {objectiveId}", objectiveId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
