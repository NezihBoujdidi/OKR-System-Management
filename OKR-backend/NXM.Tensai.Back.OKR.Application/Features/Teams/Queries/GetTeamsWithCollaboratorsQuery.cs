using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Identity;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Application.Common.Models;

namespace NXM.Tensai.Back.OKR.Application.Features.Teams.Queries;

public record GetTeamsWithCollaboratorsQuery(Guid OrganizationId) : IRequest<TeamsWithCollaboratorsResultDto>;

public class GetTeamsWithCollaboratorsQueryHandler : IRequestHandler<GetTeamsWithCollaboratorsQuery, TeamsWithCollaboratorsResultDto>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ITeamUserRepository _teamUserRepository;
    private readonly IUserRepository _userRepository;
    private readonly UserManager<User> _userManager;

    public GetTeamsWithCollaboratorsQueryHandler(
        ITeamRepository teamRepository,
        ITeamUserRepository teamUserRepository,
        IUserRepository userRepository,
        UserManager<User> userManager)
    {
        _teamRepository = teamRepository;
        _teamUserRepository = teamUserRepository;
        _userRepository = userRepository;
        _userManager = userManager;
    }

    public async Task<TeamsWithCollaboratorsResultDto> Handle(GetTeamsWithCollaboratorsQuery request, CancellationToken cancellationToken)
    {
        var teams = (await _teamRepository.GetTeamsByOrganizationIdAsync(request.OrganizationId)).ToList();
        teams = teams.Where(t => !t.IsDeleted).ToList();

        var result = new TeamsWithCollaboratorsResultDto
        {
            Teams = new List<TeamWithCollaboratorsDto>()
        };

        foreach (var team in teams)
        {
            var users = (await _teamUserRepository.GetUsersByTeamIdAsync(team.Id)).ToList();
            users = users.Where(u => u.IsEnabled).ToList();
            var userWithRoleDtos = new List<UserWithRoleDto>();
            foreach (var user in users) 
            {
                var roles = await _userManager.GetRolesAsync(user);
                string role = roles.FirstOrDefault() ?? string.Empty;
                userWithRoleDtos.Add(user.ToUserWithRoleDto(role));
            }

            result.Teams.Add(new TeamWithCollaboratorsDto
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                TeamManagerId = team.TeamManagerId,
                OrganizationId = team.OrganizationId,
                Collaborators = userWithRoleDtos
            });
        }

        return result;
    }
}
