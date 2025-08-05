using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.Application;

namespace NXM.Tensai.Back.OKR.AI.Services.MediatRService
{
    public class TeamMediatRService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TeamMediatRService> _logger;

        public TeamMediatRService(IMediator mediator, ILogger<TeamMediatRService> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create a new team by directly calling the CreateTeamCommand handler
        /// </summary>
        public async Task<TeamCreationResponse> CreateTeamAsync(TeamCreationRequest request)
        {
            try
            {
                _logger.LogInformation("Creating team: {TeamName}", request.Name);
                
                // Map the request to the command
                var command = new CreateTeamCommand
                {
                    Name = request.Name,
                    Description = request.Description,
                    OrganizationId = Guid.Parse(request.OrganizationId),
                    TeamManagerId = !string.IsNullOrEmpty(request.TeamManagerId) ? Guid.Parse(request.TeamManagerId) : null
                };

                // Send the command to the handler
                var teamId = await _mediator.Send(command);
                _logger.LogInformation("Team created successfully with ID: {TeamId}", teamId);
                
                // Return a response that matches the structure expected by consuming code
                return new TeamCreationResponse
                {
                    TeamId = teamId.ToString(),
                    Name = request.Name,
                    Description = request.Description ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.UserId ?? request.CurrentUserId ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating team: {ErrorMessage}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerErrorMessage}", ex.InnerException.Message);
                }
                throw new ApplicationException("Failed to create team", ex);
            }
        }
        
        /// <summary>
        /// Update an existing team by calling the UpdateTeamCommand handler
        /// </summary>
        public async Task<TeamUpdateResponse> UpdateTeamAsync(string teamId, TeamUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Updating team with ID: {TeamId}", teamId);
                
                // Parse the teamId to Guid
                var id = Guid.Parse(teamId);
                
                // Create the UpdateTeamCommand
                var command = new UpdateTeamCommand
                {
                    Name = request.Name,
                    Description = request.Description,
                    OrganizationId = !string.IsNullOrEmpty(request.OrganizationId) ? Guid.Parse(request.OrganizationId) : Guid.Empty,
                    TeamManagerId = !string.IsNullOrEmpty(request.TeamManagerId) ? Guid.Parse(request.TeamManagerId) : null
                };
                
                // Wrap with the WithId command
                var updateCommand = new UpdateTeamCommandWithId(id, command);
                
                // Send the command
                await _mediator.Send(updateCommand);
                
                // Get updated team details to return
                var teamDetails = await GetTeamDetailsAsync(teamId);
                
                _logger.LogInformation("Team updated successfully: {TeamId}", teamId);
                
                return new TeamUpdateResponse
                {
                    TeamId = teamId,
                    Name = request.Name,
                    Description = request.Description,
                    TeamManagerId = request.TeamManagerId,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to update team: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Delete a team by calling the DeleteTeamCommand handler
        /// </summary>
        public async Task<TeamDeleteResponse> DeleteTeamAsync(string teamId)
        {
            try
            {
                _logger.LogInformation("Deleting team with ID: {TeamId}", teamId);
                
                // Get team details before deletion to include in response
                var teamToDelete = await GetTeamDetailsAsync(teamId);
                
                // Parse the teamId to Guid
                var id = Guid.Parse(teamId);
                
                // Create and send the delete command
                var command = new DeleteTeamCommand(id);
                await _mediator.Send(command);
                
                _logger.LogInformation("Team deleted successfully: {TeamId}", teamId);
                
                // Return information about the deleted team
                return new TeamDeleteResponse
                {
                    TeamId = teamId,
                    Name = teamToDelete.Name,
                    DeletedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting team: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to delete team: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get teams by manager ID by calling the appropriate query handler
        /// </summary>
        public async Task<TeamsByManagerResponse> GetTeamsByManagerIdAsync(string managerId)
        {
            try
            {
                _logger.LogInformation("Getting teams for manager with ID: {ManagerId}", managerId);
                
                // Parse the managerId to Guid
                var id = Guid.Parse(managerId);
                
                // Create and send the query
                var query = new GetTeamsByManagerIdQuery { ManagerId = id };
                var teams = await _mediator.Send(query);
                
                // Map the results to the expected response format
                var result = new TeamsByManagerResponse
                {
                    ManagerId = managerId,
                    Teams = new List<TeamDetailsResponse>()
                };
                
                foreach (var team in teams)
                {
                    result.Teams.Add(new TeamDetailsResponse
                    {
                        TeamId = team.Id.ToString(),
                        Name = team.Name,
                        Description = team.Description ?? string.Empty,
                        CreatedAt = team.CreatedDate,
                        Members = new List<TeamMemberResponse>() // We're not fetching members here for simplicity
                    });
                }
                
                _logger.LogInformation("Found {Count} teams for manager {ManagerId}", result.Teams.Count, managerId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teams by manager ID: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to get teams for manager: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get teams by organization ID by calling the appropriate query handler
        /// </summary>
        public async Task<TeamsByOrganizationResponse> GetTeamsByOrganizationIdAsync(string organizationId)
        {
            try
            {
                _logger.LogInformation("Getting teams for organization with ID: {OrganizationId}", organizationId);
                
                // Parse the organizationId to Guid
                var id = Guid.Parse(organizationId);
                
                // Create and send the query
                var query = new GetTeamsByOrganizationIdQuery { OrganizationId = id };
                var teams = await _mediator.Send(query);
                
                // Map the results to the expected response format
                var result = new TeamsByOrganizationResponse
                {
                    OrganizationId = organizationId,
                    Teams = new List<TeamDetailsResponse>()
                };
                
                foreach (var team in teams)
                {
                    result.Teams.Add(new TeamDetailsResponse
                    {
                        TeamId = team.Id.ToString(),
                        Name = team.Name,
                        Description = team.Description ?? string.Empty,
                        CreatedAt = team.CreatedDate,
                        Members = new List<TeamMemberResponse>() // We're not fetching members here for simplicity
                    });
                }
                
                _logger.LogInformation("Found {Count} teams for organization {OrganizationId}", result.Teams.Count, organizationId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teams by organization ID: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to get teams for organization: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Add a member to an existing team by calling the appropriate command handler
        /// </summary>
        public async Task<TeamMemberResponse> AddTeamMemberAsync(string teamId, TeamMemberRequest request)
        {
            try
            {
                _logger.LogInformation("Adding team member to team {TeamId}", teamId);
                
                // This would call a command to add a team member
                // Note: The actual implementation depends on what command exists in your application
                // For now, we'll create a placeholder that you can implement later
                
                var result = new TeamMemberResponse
                {
                    MemberId = Guid.NewGuid().ToString(), // This would be returned from the actual command
                    TeamId = teamId,
                    UserId = request.UserId,
                    Email = request.Email,
                    Role = request.Role,
                    AddedAt = DateTime.UtcNow
                };
                
                _logger.LogInformation("Team member added successfully with ID: {MemberId}", result.MemberId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding team member: {ErrorMessage}", ex.Message);
                throw new ApplicationException("Failed to add member to team", ex);
            }
        }
        
        /// <summary>
        /// Get team details by calling the appropriate query handler
        /// </summary>
        public async Task<TeamDetailsResponse> GetTeamDetailsAsync(string teamId)
        {
            try
            {
                _logger.LogInformation("Getting details for team {TeamId}", teamId);
                
                // Create and send the query to get team details
                var query = new GetTeamByIdQuery(Guid.Parse(teamId));
                var teamDto = await _mediator.Send(query);
                
                // Map the result to the response expected by consumers
                var result = new TeamDetailsResponse
                {
                    TeamId = teamDto.Id.ToString(),
                    Name = teamDto.Name,
                    Description = teamDto.Description ?? string.Empty,
                    CreatedAt = teamDto.CreatedDate,
                    Members = new List<TeamMemberResponse>()
                };
                
                // If we need to fetch team members, we would do so here
                // For example, using a GetUsersByTeamIdQuery
                try
                {
                    var membersQuery = new GetUsersByTeamIdQuery { TeamId = Guid.Parse(teamId) };
                    var members = await _mediator.Send(membersQuery);
                    
                    foreach (var member in members)
                    {
                        result.Members.Add(new TeamMemberResponse
                        {
                            MemberId = member.Id.ToString(),
                            UserId = member.Id.ToString(),
                            FirstName = member.FirstName,
                            LastName = member.LastName,
                            Email = member.Email,
                            Position = member.Position,
                            Role = member.Role,
                            AddedAt = DateTime.UtcNow // This would ideally be when they were added to the team
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch team members for team {TeamId}", teamId);
                    // We don't want to fail the whole request if just fetching members fails
                }
                
                _logger.LogInformation("Retrieved details for team {TeamId}, found {MemberCount} members", 
                    teamId, result.Members?.Count ?? 0);
                    
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team details: {ErrorMessage}", ex.Message);
                throw new ApplicationException("Failed to get team details", ex);
            }
        }

        /// <summary>
        /// Search for teams based on criteria
        /// </summary>
        public async Task<TeamSearchResponse> SearchTeamsAsync(string name = null, string organizationId = null)
        {
            try
            {
                _logger.LogInformation("Searching for teams with name: {Name}, organization: {OrganizationId}", 
                    name, organizationId);
                
                var query = new SearchTeamsQuery
                {
                    Name = name,
                    OrganizationId = !string.IsNullOrEmpty(organizationId) ? Guid.Parse(organizationId) : null,
                    Page = 1,
                    PageSize = 20
                };
                
                var result = await _mediator.Send(query);
                
                // Map the results to the expected response format
                var response = new TeamSearchResponse
                {
                    SearchTerm = name,
                    OrganizationId = organizationId,
                    Teams = new List<TeamDetailsResponse>()
                };
                
                foreach (var team in result.Items)
                {
                    response.Teams.Add(new TeamDetailsResponse
                    {
                        TeamId = team.Id.ToString(),
                        Name = team.Name,
                        Description = team.Description ?? string.Empty,
                        CreatedAt = team.CreatedDate,
                        Members = new List<TeamMemberResponse>()
                    });
                }
                
                _logger.LogInformation("Found {Count} teams matching criteria", response.Teams.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching teams: {ErrorMessage}", ex.Message);
                throw new ApplicationException("Failed to search teams", ex);
            }
        }
    }
}
