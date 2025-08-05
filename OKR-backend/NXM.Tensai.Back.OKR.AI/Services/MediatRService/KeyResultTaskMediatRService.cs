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
    public class KeyResultTaskMediatRService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<KeyResultTaskMediatRService> _logger;

        public KeyResultTaskMediatRService(IMediator mediator, ILogger<KeyResultTaskMediatRService> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create a new key result task by calling the CreateKeyResultTaskCommand handler
        /// </summary>
        public async Task<KeyResultTaskCreationResponse> CreateKeyResultTaskAsync(KeyResultTaskCreationRequest request)
        {
            try
            {
                _logger.LogInformation("Creating key result task: {Title}", request.Title);
                
                // Map the request to the command
                var command = new CreateKeyResultTaskCommand
                {
                    KeyResultId = !string.IsNullOrEmpty(request.KeyResultId) ? Guid.Parse(request.KeyResultId) : Guid.Empty,
                    UserId = !string.IsNullOrEmpty(request.UserId) ? Guid.Parse(request.UserId) : Guid.Empty,
                    Title = request.Title,
                    Description = request.Description,
                    StartedDate = EnsureUtc(request.StartedDate),
                    EndDate = EnsureUtc(request.EndDate),
                    CollaboratorId = !string.IsNullOrEmpty(request.CollaboratorId) ? Guid.Parse(request.CollaboratorId) : Guid.Empty,
                    Progress = request.Progress,
                    Priority = !string.IsNullOrEmpty(request.Priority) ? Enum.Parse<Priority>(request.Priority, true) : null
                };

                // Send the command to the handler
                await _mediator.Send(command);
                
                // In the CreateKeyResultTaskCommand handler, the command creates a new KeyResultTask with a new ID,
                // but the ID is not returned. We need to search for the KeyResultTask we just created
                var searchQuery = new SearchKeyResultTasksQuery
                {
                    Title = request.Title,
                    KeyResultId = Guid.Parse(request.KeyResultId),
                    UserId = !string.IsNullOrEmpty(request.UserId) ? Guid.Parse(request.UserId) : null,
                    Page = 1,
                    PageSize = 10
                };
                
                var searchResult = await _mediator.Send(searchQuery);
                var keyResultTask = searchResult.Items.FirstOrDefault();
                
                // FIXED: Throw an exception if the task wasn't found rather than creating a fake response
                if (keyResultTask == null)
                {
                    _logger.LogError("Key result task was not found after creation. Title: {Title}, KeyResultId: {KeyResultId}", 
                        request.Title, request.KeyResultId);
                    throw new ApplicationException($"Key result task '{request.Title}' was not found in the database after creation. The database operation may have failed.");
                }
                
                string keyResultTaskId = keyResultTask.Id.ToString();
                
                _logger.LogInformation("Key result task created successfully with ID: {KeyResultTaskId}", keyResultTaskId);
                
                // Get key result details to include key result title
                string keyResultTitle = "Unknown Key Result";
                try
                {
                    var keyResultQuery = new GetKeyResultByIdQuery(Guid.Parse(request.KeyResultId));
                    var keyResult = await _mediator.Send(keyResultQuery);
                    keyResultTitle = keyResult.Title;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch key result details for task creation");
                }
                
                // Return a response that matches the structure expected by consuming code
                return new KeyResultTaskCreationResponse
                {
                    KeyResultTaskId = keyResultTaskId,
                    Title = request.Title,
                    Description = request.Description ?? string.Empty,
                    KeyResultId = request.KeyResultId,
                    KeyResultTitle = keyResultTitle,
                    UserId = request.UserId,
                    CollaboratorId = request.CollaboratorId,
                    Progress = request.Progress,
                    CreatedAt = DateTime.UtcNow,
                    PromptTemplate = $"I've created a new task '{request.Title}' for key result '{keyResultTitle}'."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating key result task: {ErrorMessage}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerErrorMessage}", ex.InnerException.Message);
                }
                throw new ApplicationException("Failed to create key result task", ex);
            }
        }
        
        /// <summary>
        /// Update an existing key result task by calling the UpdateKeyResultTaskCommand handler
        /// </summary>
        public async Task<KeyResultTaskUpdateResponse> UpdateKeyResultTaskAsync(string keyResultTaskId, KeyResultTaskUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Updating key result task with ID: {KeyResultTaskId}", keyResultTaskId);
                
                // Get the current key result task to merge with updates
                var currentKeyResultTask = await GetKeyResultTaskDetailsAsync(keyResultTaskId);
                
                // Parse the keyResultTaskId to Guid
                var id = Guid.Parse(keyResultTaskId);
                
                // Create the UpdateKeyResultTaskCommand with existing and updated values
                var command = new UpdateKeyResultTaskCommand
                {
                    KeyResultId = !string.IsNullOrEmpty(request.KeyResultId) 
                        ? Guid.Parse(request.KeyResultId) 
                        : Guid.Parse(currentKeyResultTask.KeyResultId),
                    
                    UserId = !string.IsNullOrEmpty(request.UserId) 
                        ? Guid.Parse(request.UserId) 
                        : Guid.Parse(currentKeyResultTask.UserId),
                    
                    Title = request.Title ?? currentKeyResultTask.Title,
                    Description = request.Description ?? currentKeyResultTask.Description,
                    StartedDate = request.StartedDate ?? currentKeyResultTask.StartedDate,
                    EndDate = request.EndDate ?? currentKeyResultTask.EndDate,
                    Progress = request.Progress ?? currentKeyResultTask.Progress,
                    Priority = !string.IsNullOrEmpty(request.Priority) 
                        ? Enum.Parse<Priority>(request.Priority, true) 
                        : Enum.Parse<Priority>(currentKeyResultTask.Priority, true),
                    IsDeleted = request.IsDeleted ?? false
                };
                
                // Wrap with the WithId command
                var updateCommand = new UpdateKeyResultTaskCommandWithId(id, command);
                
                // Send the command
                await _mediator.Send(updateCommand);
                
                // Get updated key result task details to return
                var keyResultTaskDetails = await GetKeyResultTaskDetailsAsync(keyResultTaskId);
                
                _logger.LogInformation("Key result task updated successfully: {KeyResultTaskId}", keyResultTaskId);
                
                return new KeyResultTaskUpdateResponse
                {
                    KeyResultTaskId = keyResultTaskId,
                    Title = command.Title,
                    Description = command.Description,
                    KeyResultId = command.KeyResultId.ToString(),
                    KeyResultTitle = keyResultTaskDetails.KeyResultTitle,
                    UserId = command.UserId.ToString(),
                    StartedDate = command.StartedDate,
                    EndDate = command.EndDate,
                    Progress = command.Progress,
                    Priority = command.Priority.ToString(),
                    UpdatedAt = DateTime.UtcNow,
                    PromptTemplate = $"I've updated the task '{command.Title}' for you."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating key result task: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to update key result task: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Delete a key result task by calling the DeleteKeyResultTaskCommand handler
        /// </summary>
        public async Task<KeyResultTaskDeleteResponse> DeleteKeyResultTaskAsync(string keyResultTaskId)
        {
            try
            {
                _logger.LogInformation("Deleting key result task with ID: {KeyResultTaskId}", keyResultTaskId);
                
                // Get key result task details before deletion to include in response
                var keyResultTaskToDelete = await GetKeyResultTaskDetailsAsync(keyResultTaskId);
                
                // Parse the keyResultTaskId to Guid
                var id = Guid.Parse(keyResultTaskId);
                
                // Create and send the delete command
                var command = new DeleteKeyResultTaskCommand(id);
                await _mediator.Send(command);
                
                _logger.LogInformation("Key result task deleted successfully: {KeyResultTaskId}", keyResultTaskId);
                
                // Return information about the deleted key result task
                return new KeyResultTaskDeleteResponse
                {
                    KeyResultTaskId = keyResultTaskId,
                    Title = keyResultTaskToDelete.Title,
                    KeyResultId = keyResultTaskToDelete.KeyResultId,
                    KeyResultTitle = keyResultTaskToDelete.KeyResultTitle,
                    DeletedAt = DateTime.UtcNow,
                    PromptTemplate = $"I've deleted the task '{keyResultTaskToDelete.Title}' for you."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting key result task: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to delete key result task: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get key result tasks by key result ID
        /// </summary>
        public async Task<KeyResultTasksByKeyResultResponse> GetKeyResultTasksByKeyResultIdAsync(string keyResultId)
        {
            try
            {
                _logger.LogInformation("Getting key result tasks for key result with ID: {KeyResultId}", keyResultId);
                
                // Create and execute query to find key result tasks for this key result
                var query = new GetKeyResultsTasksByKeyResultIdQuery(Guid.Parse(keyResultId));
                var keyResultTasks = await _mediator.Send(query);
                
                // Get key result details to include key result title
                var keyResultQuery = new GetKeyResultByIdQuery(Guid.Parse(keyResultId));
                var keyResultDto = await _mediator.Send(keyResultQuery);
                
                var result = new KeyResultTasksByKeyResultResponse
                {
                    KeyResultId = keyResultId,
                    KeyResultTitle = keyResultDto.Title,
                    KeyResultTasks = new List<KeyResultTaskDetailsResponse>()
                };
                
                // Map key result tasks to response objects
                foreach (var task in keyResultTasks)
                {
                    var taskDetails = await MapKeyResultTaskToDetailsResponse(task);
                    result.KeyResultTasks.Add(taskDetails);
                }
                
                // Set count and return
                
                // Set appropriate prompt template
                if (result.KeyResultTasks.Count == 0)
                {
                    result.PromptTemplate = $"There are no tasks for the key result '{keyResultDto.Title}'. Would you like to create one?";
                }
                else
                {
                    var tasksListBuilder = string.Join("\n", result.KeyResultTasks.Select((task, i) => 
                        $"{i+1}. {task.Title} - Progress: {task.Progress}% - Priority: {task.Priority}"));
                        
                    result.PromptTemplate = $"I found {result.KeyResultTasks.Count} tasks for key result '{keyResultDto.Title}':\n\n{tasksListBuilder}";
                }
                
                _logger.LogInformation("Found {Count} key result tasks for key result {KeyResultId}", result.KeyResultTasks.Count, keyResultId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key result tasks by key result ID: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to get key result tasks for key result: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get key result task details by calling the appropriate query handler
        /// </summary>
        public async Task<KeyResultTaskDetailsResponse> GetKeyResultTaskDetailsAsync(string keyResultTaskId)
        {
            try
            {
                _logger.LogInformation("Getting details for key result task {KeyResultTaskId}", keyResultTaskId);
                
                // Create and send the query to get key result task details
                var query = new GetKeyResultTaskByIdQuery(Guid.Parse(keyResultTaskId));
                var keyResultTaskDto = await _mediator.Send(query);
                
                var result = await MapKeyResultTaskToDetailsResponse(keyResultTaskDto);
                
                _logger.LogInformation("Retrieved details for key result task {KeyResultTaskId}", keyResultTaskId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key result task details: {ErrorMessage}", ex.Message);
                throw new ApplicationException("Failed to get key result task details", ex);
            }
        }
        
        /// <summary>
        /// Search for key result tasks based on criteria
        /// </summary>
        public async Task<KeyResultTaskSearchResponse> SearchKeyResultTasksAsync(string title = null, string keyResultId = null, string userId = null)
        {
            try
            {
                _logger.LogInformation("Searching for key result tasks with title: {Title}, keyResultId: {KeyResultId}, userId: {UserId}", 
                    title, keyResultId, userId);
                
                // Create the search query
                var searchQuery = new SearchKeyResultTasksQuery
                {
                    Title = title,
                    KeyResultId = !string.IsNullOrEmpty(keyResultId) ? Guid.Parse(keyResultId) : null,
                    UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                    Page = 1,
                    PageSize = 50
                };
                
                // Execute the search
                var result = await _mediator.Send(searchQuery);
                
                // Map the results to the expected response format
                var response = new KeyResultTaskSearchResponse
                {
                    SearchTerm = title,
                    KeyResultId = keyResultId,
                    UserId = userId,
                    KeyResultTasks = new List<KeyResultTaskDetailsResponse>()
                };
                
                // Convert DTOs to AI model responses
                foreach (var task in result.Items)
                {
                    try
                    {
                        var taskDetails = await MapKeyResultTaskToDetailsResponse(task);
                        response.KeyResultTasks.Add(taskDetails);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error mapping key result task {KeyResultTaskId}", task.Id);
                    }
                }
                
                // Generate appropriate prompt template
                string searchTerm = !string.IsNullOrEmpty(title) ? $"'{title}'" : "your criteria";
                if (response.KeyResultTasks.Count == 0)
                {
                    response.PromptTemplate = $"I couldn't find any tasks matching {searchTerm}. Would you like to create a new task?";
                }
                else
                {
                    var tasksList = string.Join("\n", response.KeyResultTasks.Select((task, i) => 
                        $"{i+1}. {task.Title} - For key result: {task.KeyResultTitle} - Progress: {task.Progress}% - Priority: {task.Priority}"));
                    
                    response.PromptTemplate = $"I found {response.KeyResultTasks.Count} tasks matching {searchTerm}:\n\n{tasksList}";
                }
                
                _logger.LogInformation("Found {Count} key result tasks matching search criteria", response.KeyResultTasks.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for key result tasks: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to search for key result tasks: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Helper method to map a key result task DTO to a details response
        /// </summary>
        private async Task<KeyResultTaskDetailsResponse> MapKeyResultTaskToDetailsResponse(KeyResultTaskDto keyResultTaskDto)
        {
            var result = new KeyResultTaskDetailsResponse
            {
                KeyResultTaskId = keyResultTaskDto.Id.ToString(),
                Title = keyResultTaskDto.Title,
                Description = keyResultTaskDto.Description ?? string.Empty,
                KeyResultId = keyResultTaskDto.KeyResultId.ToString(),
                UserId = keyResultTaskDto.UserId.ToString(),
                CollaboratorId = keyResultTaskDto.CollaboratorId.ToString(),
                StartedDate = keyResultTaskDto.StartedDate,
                EndDate = keyResultTaskDto.EndDate,
                CreatedDate = keyResultTaskDto.CreatedDate,
                ModifiedDate = keyResultTaskDto.ModifiedDate,
                Progress = keyResultTaskDto.Progress,
                Priority = keyResultTaskDto.Priority.ToString(),
                IsDeleted = keyResultTaskDto.IsDeleted,
                PromptTemplate = $"Here are the details for the task '{keyResultTaskDto.Title}'."
            };
            
            // Get key result title
            try
            {
                if (keyResultTaskDto.KeyResultId != Guid.Empty)
                {
                    var keyResultQuery = new GetKeyResultByIdQuery(keyResultTaskDto.KeyResultId);
                    var keyResult = await _mediator.Send(keyResultQuery);
                    result.KeyResultTitle = keyResult.Title;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch key result title for task {KeyResultTaskId}", keyResultTaskDto.Id);
            }
            
            // Get user name
            try
            {
                if (keyResultTaskDto.UserId != Guid.Empty)
                {
                    var userQuery = new GetUserByIdQuery { Id = keyResultTaskDto.UserId };
                    var user = await _mediator.Send(userQuery);
                    result.UserName = $"{user.FirstName} {user.LastName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch user name for task {KeyResultTaskId}", keyResultTaskDto.Id);
            }
            
            // Get collaborator name
            try
            {
                if (keyResultTaskDto.CollaboratorId != Guid.Empty)
                {
                    var collaboratorQuery = new GetUserByIdQuery { Id = keyResultTaskDto.CollaboratorId };
                    var collaborator = await _mediator.Send(collaboratorQuery);
                    result.CollaboratorName = $"{collaborator.FirstName} {collaborator.LastName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch collaborator name for task {KeyResultTaskId}", keyResultTaskDto.Id);
            }
            
            return result;
        }

        /// <summary>
        /// Helper method to ensure DateTime values are in UTC format
        /// </summary>
        private DateTime EnsureUtc(DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) : dateTime.ToUniversalTime();
        }
    }
}