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
    public class ObjectiveMediatRService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ObjectiveMediatRService> _logger;

        public ObjectiveMediatRService(IMediator mediator, ILogger<ObjectiveMediatRService> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create a new objective by calling the CreateObjectiveCommand handler
        /// </summary>
        public async Task<ObjectiveCreationResponse> CreateObjectiveAsync(ObjectiveCreationRequest request)
        {
            try
            {
                _logger.LogInformation("Creating objective: {Title}", request.Title);
                
                // Map the request to the command
                var command = new CreateObjectiveCommand
                {
                    Title = request.Title,
                    Description = request.Description,
                    OKRSessionId = !string.IsNullOrEmpty(request.OKRSessionId) ? Guid.Parse(request.OKRSessionId) : Guid.Empty,
                    ResponsibleTeamId = !string.IsNullOrEmpty(request.ResponsibleTeamId) ? Guid.Parse(request.ResponsibleTeamId) : Guid.Empty,
                    UserId = !string.IsNullOrEmpty(request.UserId) ? Guid.Parse(request.UserId) : Guid.Empty,
                    StartedDate = EnsureUtc(request.StartedDate),
                    EndDate = EnsureUtc(request.EndDate),
                    Status = !string.IsNullOrEmpty(request.Status) ? Enum.Parse<Status>(request.Status, true) : null,
                    Priority = !string.IsNullOrEmpty(request.Priority) ? Enum.Parse<Priority>(request.Priority, true) : null,
                    Progress = request.Progress
                };

                // Send the command to the handler
                var objectiveId = await _mediator.Send(command);
                _logger.LogInformation("Objective created successfully with ID: {ObjectiveId}", objectiveId);
                
                // Return a response that matches the structure expected by consuming code
                return new ObjectiveCreationResponse
                {
                    ObjectiveId = objectiveId.ToString(),
                    Title = request.Title,
                    Description = request.Description ?? string.Empty,
                    OKRSessionId = request.OKRSessionId,
                    ResponsibleTeamId = request.ResponsibleTeamId,
                    UserId = request.UserId,
                    StartedDate = command.StartedDate,
                    EndDate = command.EndDate,
                    CreatedDate = DateTime.UtcNow,
                    Status = request.Status,
                    Priority = request.Priority,
                    Progress = request.Progress,
                    PromptTemplate = $"I've created a new objective '{request.Title}' for you."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating objective: {ErrorMessage}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerErrorMessage}", ex.InnerException.Message);
                }
                throw new ApplicationException("Failed to create objective", ex);
            }
        }
        
        /// <summary>
        /// Update an existing objective by calling the UpdateObjectiveCommand handler
        /// </summary>
        public async Task<ObjectiveUpdateResponse> UpdateObjectiveAsync(string objectiveId, ObjectiveUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Updating objective with ID: {ObjectiveId}", objectiveId);
                
                // Parse the objectiveId to Guid
                var id = Guid.Parse(objectiveId);
                
                // Parse OKRSessionId and UserId if provided
                Guid okrSessionId = Guid.Empty;
                if (!string.IsNullOrEmpty(request.OKRSessionId))
                {
                    okrSessionId = Guid.Parse(request.OKRSessionId);
                    _logger.LogDebug("Using OKRSessionId: {OKRSessionId}", request.OKRSessionId);
                }

                Guid userId = Guid.Empty;
                if (!string.IsNullOrEmpty(request.UserId))
                {
                    userId = Guid.Parse(request.UserId);
                    _logger.LogDebug("Using UserId: {UserId}", request.UserId);
                }
                
                // Create the UpdateObjectiveCommand with all properties in the initializer, including the init-only properties
                var command = new UpdateObjectiveCommand
                {
                    Title = request.Title,
                    Description = request.Description,
                    ResponsibleTeamId = !string.IsNullOrEmpty(request.ResponsibleTeamId) ? Guid.Parse(request.ResponsibleTeamId) : Guid.Empty,
                    StartedDate = EnsureUtc(request.StartedDate),
                    EndDate = EnsureUtc(request.EndDate),
                    Status = !string.IsNullOrEmpty(request.Status) ? Enum.Parse<Status>(request.Status, true) : Status.NotStarted, // Default to 'NotStarted'
                    Priority = !string.IsNullOrEmpty(request.Priority) ? Enum.Parse<Priority>(request.Priority, true) : Priority.Low,
                    Progress = request.Progress,
                    OKRSessionId = okrSessionId,
                    UserId = userId
                };
                
                // Wrap with the WithId command
                var updateCommand = new UpdateObjectiveCommandWithId(id, command);
                
                // Send the command
                await _mediator.Send(updateCommand);
                
                // Get updated objective details to return
                var objectiveDetails = await GetObjectiveDetailsAsync(objectiveId);
                
                _logger.LogInformation("Objective updated successfully: {ObjectiveId}", objectiveId);
                
                return new ObjectiveUpdateResponse
                {
                    ObjectiveId = objectiveId,
                    Title = request.Title,
                    Description = request.Description,
                    OKRSessionId = objectiveDetails.OKRSessionId,
                    ResponsibleTeamId = request.ResponsibleTeamId,
                    StartedDate = command.StartedDate,
                    EndDate = command.EndDate,
                    ModifiedDate = DateTime.UtcNow,
                    Status = request.Status,
                    Priority = request.Priority,
                    Progress = request.Progress ?? objectiveDetails.Progress,
                    PromptTemplate = $"I've updated the objective '{request.Title}' for you."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating objective: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to update objective: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Delete an objective by calling the DeleteObjectiveCommand handler
        /// </summary>
        public async Task<ObjectiveDeleteResponse> DeleteObjectiveAsync(string objectiveId, string userId)
        {
            try
            {
                _logger.LogInformation("Deleting objective with ID: {ObjectiveId}", objectiveId);
                
                // Get objective details before deletion to include in response
                var objectiveToDelete = await GetObjectiveDetailsAsync(objectiveId);
                
                // Parse the objectiveId to Guid
                var id = Guid.Parse(objectiveId);
                
                // Create and send the delete command
                var command = new DeleteObjectiveCommand(id);
                await _mediator.Send(command);
                
                _logger.LogInformation("Objective deleted successfully: {ObjectiveId}", objectiveId);
                
                // Return information about the deleted objective
                return new ObjectiveDeleteResponse
                {
                    ObjectiveId = objectiveId,
                    Title = objectiveToDelete.Title,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = userId,
                    PromptTemplate = $"I've deleted the objective '{objectiveToDelete.Title}' for you."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting objective: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to delete objective: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get objectives by OKR session ID
        /// </summary>
        public async Task<ObjectivesBySessionResponse> GetObjectivesBySessionIdAsync(string okrSessionId)
        {
            try
            {
                _logger.LogInformation("Getting objectives for OKR session with ID: {OkrSessionId}", okrSessionId);
                
                // Create and execute query to find objectives for this session
                var query = new GetObjectivesBySessionIdQuery(Guid.Parse(okrSessionId));
                var objectives = await _mediator.Send(query);
                
                // Get OKR session details to include session title
                var sessionQuery = new GetOKRSessionByIdQuery(Guid.Parse(okrSessionId));
                var sessionDto = await _mediator.Send(sessionQuery);
                
                var result = new ObjectivesBySessionResponse
                {
                    OKRSessionId = okrSessionId,
                    OKRSessionTitle = sessionDto.Title,
                    Objectives = new List<ObjectiveDetailsResponse>()
                };
                
                // Map objectives to response objects
                foreach (var objective in objectives)
                {
                    var objectiveDetails = await MapObjectiveToDetailsResponse(objective);
                    result.Objectives.Add(objectiveDetails);
                }
                
                // Set appropriate prompt template
                if (result.Objectives.Count == 0)
                {
                    result.PromptTemplate = $"There are no objectives for the OKR session '{sessionDto.Title}'. Would you like to create one?";
                }
                else
                {
                    result.PromptTemplate = $"I found {result.Objectives.Count} objectives for the OKR session '{sessionDto.Title}'.";
                }
                
                _logger.LogInformation("Found {Count} objectives for OKR session {OkrSessionId}", result.Objectives.Count, okrSessionId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting objectives by OKR session ID: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to get objectives for OKR session: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get objective details by calling the appropriate query handler
        /// </summary>
        public async Task<ObjectiveDetailsResponse> GetObjectiveDetailsAsync(string objectiveId)
        {
            try
            {
                _logger.LogInformation("Getting details for objective {ObjectiveId}", objectiveId);
                
                // Create and send the query to get objective details
                var query = new GetObjectiveByIdQuery(Guid.Parse(objectiveId));
                var objectiveDto = await _mediator.Send(query);
                
                var result = await MapObjectiveToDetailsResponse(objectiveDto);
                
                _logger.LogInformation("Retrieved details for objective {ObjectiveId}", objectiveId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting objective details: {ErrorMessage}", ex.Message);
                throw new ApplicationException("Failed to get objective details", ex);
            }
        }

        /// <summary>
        /// Helper method to map an objective DTO to a details response
        /// </summary>
        private async Task<ObjectiveDetailsResponse> MapObjectiveToDetailsResponse(ObjectiveDto objectiveDto)
        {
            var result = new ObjectiveDetailsResponse
            {
                ObjectiveId = objectiveDto.Id.ToString(),
                Title = objectiveDto.Title,
                Description = objectiveDto.Description ?? string.Empty,
                OKRSessionId = objectiveDto.OKRSessionId.ToString(),
                ResponsibleTeamId = objectiveDto.ResponsibleTeamId.ToString(),
                UserId = objectiveDto.UserId.ToString(),
                StartedDate = objectiveDto.StartedDate,
                EndDate = objectiveDto.EndDate,
                CreatedDate = objectiveDto.CreatedDate,
                ModifiedDate = objectiveDto.ModifiedDate,
                Status = objectiveDto.Status.ToString(),
                Priority = objectiveDto.Priority.ToString(),
                Progress = objectiveDto.Progress
            };
            
            // Get OKR session title
            try
            {
                if (objectiveDto.OKRSessionId != Guid.Empty)
                {
                    var sessionQuery = new GetOKRSessionByIdQuery(objectiveDto.OKRSessionId);
                    var session = await _mediator.Send(sessionQuery);
                    result.OKRSessionTitle = session.Title;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch OKR session title for objective {ObjectiveId}", objectiveDto.Id);
            }
            
            // Get responsible team name
            try
            {
                if (objectiveDto.ResponsibleTeamId != Guid.Empty)
                {
                    var teamQuery = new GetTeamByIdQuery(objectiveDto.ResponsibleTeamId);
                    var team = await _mediator.Send(teamQuery);
                    result.ResponsibleTeamName = team.Name;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch responsible team name for objective {ObjectiveId}", objectiveDto.Id);
            }
            
            // Get user name
            try
            {
                if (objectiveDto.UserId != Guid.Empty)
                {
                    var userQuery = new GetUserByIdQuery { Id = objectiveDto.UserId };
                    var user = await _mediator.Send(userQuery);
                    result.UserName = $"{user.FirstName} {user.LastName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch user name for objective {ObjectiveId}", objectiveDto.Id);
            }
            
            return result;
        }

        /// <summary>
        /// Search for objectives based on criteria
        /// </summary>
        public async Task<ObjectiveSearchResponse> SearchObjectivesAsync(string title = null, string okrSessionId = null, string teamId = null, string userId = null)
        {
            try
            {
                _logger.LogInformation("Searching for objectives with title: {Title}, sessionId: {SessionId}, teamId: {TeamId}, userId: {UserId}", 
                    title, okrSessionId, teamId, userId);
                
                // Create search query with available filters
                var query = new SearchObjectivesQuery
                {
                    Title = title,
                    OKRSessionId = !string.IsNullOrEmpty(okrSessionId) ? Guid.Parse(okrSessionId) : null,
                    UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                    Page = 1,
                    PageSize = 20
                };
                
                var result = await _mediator.Send(query);
                
                // Map the results to the expected response format
                var response = new ObjectiveSearchResponse
                {
                    SearchTerm = title,
                    Objectives = new List<ObjectiveDetailsResponse>()
                };
                
                // Convert DTOs to AI model responses
                foreach (var objective in result.Items)
                {
                    try
                    {
                        var objectiveDetails = await MapObjectiveToDetailsResponse(objective);
                        response.Objectives.Add(objectiveDetails);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error getting details for objective {ObjectiveId}", objective.Id);
                    }
                }
                
                // Set appropriate prompt template
                if (response.Objectives.Count == 0)
                {
                    response.PromptTemplate = "I couldn't find any objectives matching your criteria. Would you like to create a new one?";
                }
                else
                {
                    response.PromptTemplate = $"I found {response.Objectives.Count} objectives matching your search criteria.";
                }
                
                _logger.LogInformation("Found {Count} objectives matching criteria", response.Objectives.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching objectives: {ErrorMessage}", ex.Message);
                throw new ApplicationException("Failed to search objectives", ex);
            }
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