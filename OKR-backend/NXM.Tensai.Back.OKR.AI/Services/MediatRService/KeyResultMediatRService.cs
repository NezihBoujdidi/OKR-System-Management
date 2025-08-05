using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.AI.Services.MediatRService
{
    public class KeyResultMediatRService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<KeyResultMediatRService> _logger;

        public KeyResultMediatRService(IMediator mediator, ILogger<KeyResultMediatRService> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create a new key result by calling the CreateKeyResultCommand handler
        /// </summary>
        public async Task<KeyResultCreationResponse> CreateKeyResultAsync(KeyResultCreationRequest request)
        {
            try
            {
                _logger.LogInformation("Creating key result: {Title}", request.Title);
                
                // Map the request to the command
                var command = new CreateKeyResultCommand
                {
                    ObjectiveId = !string.IsNullOrEmpty(request.ObjectiveId) ? Guid.Parse(request.ObjectiveId) : Guid.Empty,
                    UserId = !string.IsNullOrEmpty(request.UserId) ? Guid.Parse(request.UserId) : Guid.Empty,
                    Title = request.Title,
                    Description = request.Description,
                    StartedDate = EnsureUtc(request.StartedDate),
                    EndDate = EnsureUtc(request.EndDate),
                    Status = !string.IsNullOrEmpty(request.Status) ? Enum.Parse<Status>(request.Status, true) : null,
                    Progress = request.Progress
                };

                // Send the command to the handler
                await _mediator.Send(command);
                
                // In the CreateKeyResultCommand handler, the command creates a new KeyResult with a new ID,
                // but the ID is not returned. We need to search for the KeyResult we just created
                var searchQuery = new SearchKeyResultsQuery
                {
                    Title = request.Title,
                    ObjectiveId = Guid.Parse(request.ObjectiveId),
                    UserId = !string.IsNullOrEmpty(request.UserId) ? Guid.Parse(request.UserId) : null,
                    Page = 1,
                    PageSize = 10
                };
                
                var searchResult = await _mediator.Send(searchQuery);
                var keyResult = searchResult.Items.FirstOrDefault();
                string keyResultId = keyResult?.Id.ToString() ?? Guid.NewGuid().ToString();  // Fallback if not found
                
                _logger.LogInformation("Key result created successfully with ID: {KeyResultId}", keyResultId);
                
                // Get objective details for the prompt template
                string objectiveTitle = "Unknown Objective";
                try
                {
                    var objectiveQuery = new GetObjectiveByIdQuery(Guid.Parse(request.ObjectiveId));
                    var objective = await _mediator.Send(objectiveQuery);
                    objectiveTitle = objective.Title;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch objective details for key result creation");
                }
                
                // Return a response that matches the structure expected by consuming code
                return new KeyResultCreationResponse
                {
                    KeyResultId = keyResultId,
                    Title = request.Title,
                    Description = request.Description ?? string.Empty,
                    ObjectiveId = request.ObjectiveId,
                    UserId = request.UserId,
                    StartedDate = command.StartedDate,
                    EndDate = command.EndDate,
                    Status = request.Status ?? "NotStarted",
                    Progress = request.Progress,
                    CreatedAt = DateTime.UtcNow,
                    PromptTemplate = $"I've created a new key result '{request.Title}' for objective '{objectiveTitle}'."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating key result: {ErrorMessage}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerErrorMessage}", ex.InnerException.Message);
                }
                throw new ApplicationException("Failed to create key result", ex);
            }
        }
        
        /// <summary>
        /// Update an existing key result by calling the UpdateKeyResultCommand handler
        /// </summary>
        public async Task<KeyResultUpdateResponse> UpdateKeyResultAsync(string keyResultId, KeyResultUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Updating key result with ID: {KeyResultId}", keyResultId);
                
                // Parse the keyResultId to Guid
                var id = Guid.Parse(keyResultId);
                
                // Create the UpdateKeyResultCommand
                var command = new UpdateKeyResultCommand
                {
                    ObjectiveId = !string.IsNullOrEmpty(request.ObjectiveId) ? Guid.Parse(request.ObjectiveId) : Guid.Empty,
                    UserId = !string.IsNullOrEmpty(request.UserId) ? Guid.Parse(request.UserId) : Guid.Empty,
                    Title = request.Title,
                    Description = request.Description,
                    StartedDate = EnsureUtc(request.StartedDate),
                    EndDate = EnsureUtc(request.EndDate),
                    Progress = request.Progress
                };
                
                // Wrap with the WithId command
                var updateCommand = new UpdateKeyResultCommandWithId(id, command);
                
                // Send the command
                await _mediator.Send(updateCommand);
                
                // Get updated key result details to return
                var keyResultDetails = await GetKeyResultDetailsAsync(keyResultId);
                
                _logger.LogInformation("Key result updated successfully: {KeyResultId}", keyResultId);
                
                return new KeyResultUpdateResponse
                {
                    KeyResultId = keyResultId,
                    Title = request.Title,
                    Description = request.Description,
                    ObjectiveId = request.ObjectiveId,
                    ObjectiveTitle = keyResultDetails.ObjectiveTitle,
                    UserId = request.UserId,
                    StartedDate = command.StartedDate,
                    EndDate = command.EndDate,
                    Status = request.Status,
                    Progress = request.Progress,
                    UpdatedAt = DateTime.UtcNow,
                    PromptTemplate = $"I've updated the key result '{request.Title}' for you."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating key result: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to update key result: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Delete a key result by calling the DeleteKeyResultCommand handler
        /// </summary>
        public async Task<KeyResultDeleteResponse> DeleteKeyResultAsync(string keyResultId)
        {
            try
            {
                _logger.LogInformation("Deleting key result with ID: {KeyResultId}", keyResultId);
                
                // Get key result details before deletion to include in response
                var keyResultToDelete = await GetKeyResultDetailsAsync(keyResultId);
                
                // Parse the keyResultId to Guid
                var id = Guid.Parse(keyResultId);
                
                // Create and send the delete command
                var command = new DeleteKeyResultCommand(id);
                await _mediator.Send(command);
                
                _logger.LogInformation("Key result deleted successfully: {KeyResultId}", keyResultId);
                
                // Return information about the deleted key result
                return new KeyResultDeleteResponse
                {
                    KeyResultId = keyResultId,
                    Title = keyResultToDelete.Title,
                    DeletedAt = DateTime.UtcNow,
                    PromptTemplate = $"I've deleted the key result '{keyResultToDelete.Title}' for you."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting key result: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to delete key result: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get key results by objective ID
        /// </summary>
        public async Task<KeyResultsByObjectiveResponse> GetKeyResultsByObjectiveIdAsync(string objectiveId)
        {
            try
            {
                _logger.LogInformation("Getting key results for objective with ID: {ObjectiveId}", objectiveId);
                
                // Create and execute query to find key results for this objective
                var query = new GetKeyResultsByObjectiveIdQuery(Guid.Parse(objectiveId));
                var keyResults = await _mediator.Send(query);
                
                // Get objective details to include objective title
                var objectiveQuery = new GetObjectiveByIdQuery(Guid.Parse(objectiveId));
                var objectiveDto = await _mediator.Send(objectiveQuery);
                
                var result = new KeyResultsByObjectiveResponse
                {
                    ObjectiveId = objectiveId,
                    ObjectiveTitle = objectiveDto.Title,
                    KeyResults = new List<KeyResultDetailsResponse>()
                };
                
                // Map key results to response objects
                foreach (var keyResult in keyResults)
                {
                    var keyResultDetails = await MapKeyResultToDetailsResponse(keyResult);
                    result.KeyResults.Add(keyResultDetails);
                }
                
                // Set count and return
                result.Count = result.KeyResults.Count;
                
                // Set appropriate prompt template
                if (result.KeyResults.Count == 0)
                {
                    result.PromptTemplate = $"There are no key results for the objective '{objectiveDto.Title}'. Would you like to create one?";
                }
                else
                {
                    var keyResultsList = string.Join("\n", result.KeyResults.Select((kr, i) => 
                        $"{i+1}. {kr.Title} - Progress: {kr.Progress}% - {kr.Status}"));
                        
                    result.PromptTemplate = $"I found {result.KeyResults.Count} key results for objective '{objectiveDto.Title}':\n\n{keyResultsList}";
                }
                
                _logger.LogInformation("Found {Count} key results for objective {ObjectiveId}", result.KeyResults.Count, objectiveId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key results by objective ID: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to get key results for objective: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get key result details by calling the appropriate query handler
        /// </summary>
        public async Task<KeyResultDetailsResponse> GetKeyResultDetailsAsync(string keyResultId)
        {
            try
            {
                _logger.LogInformation("Getting details for key result {KeyResultId}", keyResultId);
                
                // Create and send the query to get key result details
                var query = new GetKeyResultByIdQuery(Guid.Parse(keyResultId));
                var keyResultDto = await _mediator.Send(query);
                
                var result = await MapKeyResultToDetailsResponse(keyResultDto);
                
                _logger.LogInformation("Retrieved details for key result {KeyResultId}", keyResultId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key result details: {ErrorMessage}", ex.Message);
                throw new ApplicationException("Failed to get key result details", ex);
            }
        }
        
        /// <summary>
        /// Search for key results based on criteria
        /// </summary>
        public async Task<KeyResultSearchResponse> SearchKeyResultsAsync(string title = null, string objectiveId = null, string userId = null)
        {
            try
            {
                _logger.LogInformation("Searching for key results with title: {Title}, objectiveId: {ObjectiveId}, userId: {UserId}", 
                    title, objectiveId, userId);
                
                // Create the search query
                var searchQuery = new SearchKeyResultsQuery
                {
                    Title = title,
                    ObjectiveId = !string.IsNullOrEmpty(objectiveId) ? Guid.Parse(objectiveId) : null,
                    UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                    Page = 1,
                    PageSize = 50
                };
                
                // Execute the search
                var result = await _mediator.Send(searchQuery);
                
                // Map the results to the expected response format
                var response = new KeyResultSearchResponse
                {
                    Count = result.Items.Count, // Fixed: Using Items.Count instead of TotalCount
                    KeyResults = new List<KeyResultDetailsResponse>()
                };
                
                // Convert DTOs to AI model responses
                foreach (var keyResult in result.Items)
                {
                    try
                    {
                        var keyResultDetails = await MapKeyResultToDetailsResponse(keyResult);
                        response.KeyResults.Add(keyResultDetails);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error mapping key result {KeyResultId}", keyResult.Id);
                    }
                }
                
                // Generate appropriate prompt template
                string searchTerm = !string.IsNullOrEmpty(title) ? $"'{title}'" : "your criteria";
                if (response.KeyResults.Count == 0)
                {
                    response.PromptTemplate = $"I couldn't find any key results matching {searchTerm}. Would you like to create a new key result?";
                }
                else
                {
                    var keyResultsList = string.Join("\n", response.KeyResults.Select((kr, i) => 
                        $"{i+1}. {kr.Title} - For objective: {kr.ObjectiveTitle} - Progress: {kr.Progress}%"));
                        
                    response.PromptTemplate = $"I found {response.KeyResults.Count} key results matching {searchTerm}:\n\n{keyResultsList}";
                }
                
                _logger.LogInformation("Found {Count} key results matching search criteria", response.KeyResults.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for key results: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to search for key results: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Helper method to map a key result DTO to a details response
        /// </summary>
        private async Task<KeyResultDetailsResponse> MapKeyResultToDetailsResponse(KeyResultDto keyResultDto)
        {
            var result = new KeyResultDetailsResponse
            {
                KeyResultId = keyResultDto.Id.ToString(),
                Title = keyResultDto.Title,
                Description = keyResultDto.Description ?? string.Empty,
                ObjectiveId = keyResultDto.ObjectiveId.ToString(),
                UserId = keyResultDto.UserId.ToString(),
                StartedDate = keyResultDto.StartedDate,
                EndDate = keyResultDto.EndDate,
                Status = "NotStarted", // Default value since KeyResultDto doesn't have Status
                Progress = keyResultDto.Progress
            };
            
            // Get objective title
            try
            {
                if (keyResultDto.ObjectiveId != Guid.Empty)
                {
                    var objectiveQuery = new GetObjectiveByIdQuery(keyResultDto.ObjectiveId);
                    var objective = await _mediator.Send(objectiveQuery);
                    result.ObjectiveTitle = objective.Title;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch objective title for key result {KeyResultId}", keyResultDto.Id);
            }
            
            // Get user name
            try
            {
                if (keyResultDto.UserId != Guid.Empty)
                {
                    var userQuery = new GetUserByIdQuery { Id = keyResultDto.UserId };
                    var user = await _mediator.Send(userQuery);
                    result.UserName = $"{user.FirstName} {user.LastName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch user name for key result {KeyResultId}", keyResultDto.Id);
            }
            
            return result;
        }

        /// <summary>
        /// Helper method to ensure DateTime values are in UTC format
        /// </summary>
        private DateTime EnsureUtc(DateTime dateTime)
        {
            // If the Kind is already UTC, return it as is
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime;
            }
            
            // If the Kind is Local, convert to UTC
            if (dateTime.Kind == DateTimeKind.Local)
            {
                return dateTime.ToUniversalTime();
            }
            
            // If the Kind is Unspecified, specify it as UTC
            // This assumes the time is already in UTC but just not marked as such
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
    }
}