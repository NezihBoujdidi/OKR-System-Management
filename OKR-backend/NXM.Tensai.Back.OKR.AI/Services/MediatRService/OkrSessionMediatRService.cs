using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.AI.Services.MediatRService
{
    public class OkrSessionMediatRService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<OkrSessionMediatRService> _logger;

        public OkrSessionMediatRService(IMediator mediator, ILogger<OkrSessionMediatRService> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create a new OKR session by directly calling the CreateOKRSessionCommand handler
        /// </summary>
        public async Task<OkrSessionCreationResponse> CreateOkrSessionAsync(OkrSessionCreationRequest request)
        {
            try
            {
                _logger.LogInformation("Creating OKR session: {Title}", request.Title);
                
                // Map the request to the command
                var command = new CreateOKRSessionCommand
                {
                    Title = request.Title,
                    OrganizationId = Guid.Parse(request.OrganizationId),
                    Description = request.Description,
                    StartedDate = EnsureUtc(request.StartedDate),
                    EndDate = EnsureUtc(request.EndDate),
                    // TeamIds = request.TeamIds?.Select(id => Guid.Parse(id)).ToList() ?? new List<Guid>(),
                    UserId = !string.IsNullOrEmpty(request.UserId) ? Guid.Parse(request.UserId) : Guid.Empty,
                    Color = request.Color,
                    Status = !string.IsNullOrEmpty(request.Status) ? Enum.Parse<Status>(request.Status, true) : null
                };

                // Send the command to the handler
                var okrSessionId = await _mediator.Send(command);
                _logger.LogInformation("OKR session created successfully with ID: {OkrSessionId}", okrSessionId);
                
                // Return a response that matches the structure expected by consuming code
                return new OkrSessionCreationResponse
                {
                    OkrSessionId = okrSessionId.ToString(),
                    Title = request.Title,
                    Description = request.Description ?? string.Empty,
                    StartedDate = command.StartedDate,
                    EndDate = command.EndDate,
                    //TeamManagerId = request.TeamManagerId,
                    TeamIds = request.TeamIds,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.UserId,
                    Color = request.Color,
                    Status = request.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating OKR session: {ErrorMessage}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerErrorMessage}", ex.InnerException.Message);
                }
                throw new ApplicationException("Failed to create OKR session", ex);
            }
        }
        
        /// <summary>
        /// Update an existing OKR session by calling the UpdateOKRSessionCommand handler
        /// </summary>
        public async Task<OkrSessionUpdateResponse> UpdateOkrSessionAsync(string okrSessionId, OkrSessionUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Updating OKR session with ID: {OkrSessionId}", okrSessionId);
                
                // Parse the okrSessionId to Guid
                var id = Guid.Parse(okrSessionId);
                
                // Create the UpdateOKRSessionCommand
                var command = new UpdateOKRSessionCommand
                {
                    UserId = !string.IsNullOrEmpty(request.UserId) ? Guid.Parse(request.UserId) : Guid.Empty,
                    Title = request.Title,
                    Description = request.Description,
                    StartedDate = EnsureUtc(request.StartedDate),
                    EndDate = EnsureUtc(request.EndDate),
                    //TeamManagerId = !string.IsNullOrEmpty(request.TeamManagerId) ? Guid.Parse(request.TeamManagerId) : Guid.Empty,
                    Color = request.Color,
                    Status = !string.IsNullOrEmpty(request.Status) ? Enum.Parse<Status>(request.Status, true) : null
                };
                
                // Wrap with the WithId command
                var updateCommand = new UpdateOKRSessionCommandWithId(id, command);
                
                // Send the command
                await _mediator.Send(updateCommand);
                
                // Get updated OKR session details to return
                var sessionDetails = await GetOkrSessionDetailsAsync(okrSessionId);
                
                _logger.LogInformation("OKR session updated successfully: {OkrSessionId}", okrSessionId);
                
                return new OkrSessionUpdateResponse
                {
                    OkrSessionId = okrSessionId,
                    Title = request.Title,
                    Description = request.Description,
                    StartedDate = command.StartedDate,
                    EndDate = command.EndDate,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating OKR session: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to update OKR session: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Delete an OKR session by calling the DeleteOKRSessionCommand handler
        /// </summary>
        public async Task<OkrSessionDeleteResponse> DeleteOkrSessionAsync(string okrSessionId)
        {
            try
            {
                _logger.LogInformation("Deleting OKR session with ID: {OkrSessionId}", okrSessionId);
                
                // Get OKR session details before deletion to include in response
                var sessionToDelete = await GetOkrSessionDetailsAsync(okrSessionId);
                
                // Parse the okrSessionId to Guid
                var id = Guid.Parse(okrSessionId);
                
                // Create and send the delete command
                var command = new DeleteOKRSessionCommand(id);
                await _mediator.Send(command);
                
                _logger.LogInformation("OKR session deleted successfully: {OkrSessionId}", okrSessionId);
                
                // Return information about the deleted OKR session
                return new OkrSessionDeleteResponse
                {
                    OkrSessionId = okrSessionId,
                    Title = sessionToDelete.Title,
                    DeletedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting OKR session: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to delete OKR session: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get OKR sessions by team ID
        /// </summary>
        public async Task<OkrSessionsByTeamResponse> GetOkrSessionsByTeamIdAsync(string teamId)
        {
            try
            {
                _logger.LogInformation("Getting OKR sessions for team with ID: {TeamId}", teamId);
                
                // Create and execute search query to find sessions associated with this team
                var searchQuery = new SearchOKRSessionsQuery
                {
                    Page = 1,
                    PageSize = 50 // Reasonable limit
                };
                
                var allSessions = await _mediator.Send(searchQuery);
                
                // TODO: Add a proper query to filter by team ID in your application layer
                // For now, we'll get all and filter client-side
                var result = new OkrSessionsByTeamResponse
                {
                    TeamId = teamId,
                    Sessions = new List<OkrSessionDetailsResponse>()
                };
                
                // Get team name (optional)
                try
                {
                    var teamQuery = new GetTeamByIdQuery(Guid.Parse(teamId));
                    var team = await _mediator.Send(teamQuery);
                    result.TeamName = team.Name;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve team name for team {TeamId}", teamId);
                    result.TeamName = $"Team {teamId}";
                }
                
                // Convert DTOs to AI model responses
                foreach (var session in allSessions.Items)
                {
                    try
                    {
                        var okrSession = await GetOkrSessionDetailsAsync(session.Id.ToString());
                        result.Sessions.Add(okrSession);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error getting details for OKR session {SessionId}", session.Id);
                    }
                }
                
                _logger.LogInformation("Found {Count} OKR sessions for team {TeamId}", result.Sessions.Count, teamId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OKR sessions by team ID: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to get OKR sessions for team: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get OKR session details by calling the appropriate query handler
        /// </summary>
        public async Task<OkrSessionDetailsResponse> GetOkrSessionDetailsAsync(string okrSessionId)
        {
            try
            {
                _logger.LogInformation("Getting details for OKR session {OkrSessionId}", okrSessionId);
                
                // Create and send the query to get OKR session details
                var query = new GetOKRSessionByIdQuery(Guid.Parse(okrSessionId));
                var sessionDto = await _mediator.Send(query);
                
                // Map the result to the response expected by consumers
                var result = new OkrSessionDetailsResponse
                {
                    OkrSessionId = sessionDto.Id.ToString(),
                    Title = sessionDto.Title,
                    Description = sessionDto.Description ?? string.Empty,
                    StartDate = sessionDto.StartedDate,
                    EndDate = sessionDto.EndDate,
                    //TeamManagerId = sessionDto.TeamManagerId.ToString(),
                    CreatedBy = sessionDto.UserId.ToString(),
                    CreatedAt = sessionDto.CreatedDate,
                    Color = sessionDto.Color,
                    Status = sessionDto.Status,
                    Progress = sessionDto.Progress ?? 0
                };
                
                // Get team manager name if applicable
                /* try
                {
                    if (sessionDto.TeamManagerId != Guid.Empty)
                    {   
                        // var query = new GetUserByIdQuery { Id = id };
                        // var user = await _mediator.Send(query, cancellationToken);
                        var userQuery = new GetUserByIdQuery { Id = sessionDto.TeamManagerId };
                        var user = await _mediator.Send(userQuery);
                        result.TeamManagerName = $"{user.FirstName} {user.LastName}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch team manager name for OKR session {OkrSessionId}", okrSessionId);
                } */
                
                _logger.LogInformation("Retrieved details for OKR session {OkrSessionId}", okrSessionId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OKR session details: {ErrorMessage}", ex.Message);
                throw new ApplicationException("Failed to get OKR session details", ex);
            }
        }

        /// <summary>
        /// Get all OKR sessions without any filtering
        /// </summary>
        public async Task<OkrSessionSearchResponse> GetAllOkrSessionsAsync(string userId = null)
        {
            try
            {
                _logger.LogInformation("Getting all OKR sessions for userId: {UserId}", 
                    !string.IsNullOrEmpty(userId) ? userId : "any");
                
                var query = new SearchOKRSessionsQuery
                {
                    // Explicitly set Title to null to ensure no filtering by title
                    Title = null,
                    UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                    Page = 1,
                    PageSize = 50 // Using a larger page size for "get all" requests
                };
                
                var result = await _mediator.Send(query);
                
                // Map the results to the expected response format
                var response = new OkrSessionSearchResponse
                {
                    SearchTerm = null, // Explicitly indicate this is not a search
                    Sessions = new List<OkrSessionDetailsResponse>()
                };
                
                // Convert DTOs to AI model responses
                foreach (var session in result.Items)
                {
                    try
                    {
                        var okrSession = await GetOkrSessionDetailsAsync(session.Id.ToString());
                        response.Sessions.Add(okrSession);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error getting details for OKR session {SessionId}", session.Id);
                    }
                }
                
                _logger.LogInformation("Found {Count} total OKR sessions", response.Sessions.Count);
                
                // Add a specific template for "get all" results
                if (response.Sessions.Count == 0)
                {
                    response.PromptTemplate = "I couldn't find any OKR sessions. Would you like to create a new one?";
                }
                else
                {
                    response.PromptTemplate = $"I found {response.Sessions.Count} OKR sessions.";
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all OKR sessions: {ErrorMessage}", ex.Message);
                throw new ApplicationException("Failed to get all OKR sessions", ex);
            }
        }

        /// <summary>
        /// Search for OKR sessions based on criteria
        /// </summary>
        public async Task<OkrSessionSearchResponse> SearchOkrSessionsAsync(string title = null, string userId = null)
        {
            try
            {
                _logger.LogInformation("Searching for OKR sessions with title: {Title}, userId: {UserId}", 
                    title, userId);
                
                var query = new SearchOKRSessionsQuery
                {
                    Title = title,
                    UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                    Page = 1,
                    PageSize = 20
                };
                
                var result = await _mediator.Send(query);
                
                // Map the results to the expected response format
                var response = new OkrSessionSearchResponse
                {
                    SearchTerm = title,
                    Sessions = new List<OkrSessionDetailsResponse>()
                };
                
                // Convert DTOs to AI model responses
                foreach (var session in result.Items)
                {
                    try
                    {
                        var okrSession = await GetOkrSessionDetailsAsync(session.Id.ToString());
                        response.Sessions.Add(okrSession);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error getting details for OKR session {SessionId}", session.Id);
                    }
                }
                
                _logger.LogInformation("Found {Count} OKR sessions matching criteria", response.Sessions.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching OKR sessions: {ErrorMessage}", ex.Message);
                throw new ApplicationException("Failed to search OKR sessions", ex);
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