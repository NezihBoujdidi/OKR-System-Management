using MediatR;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;

public class AddUsersToTeamCommand : IRequest<AddUsersToTeamResult>
{
    public Guid TeamId { get; set; }
    public List<Guid> UserIds { get; set; } = new();
}

public class AddUsersToTeamResult
{
    public List<Guid> AddedUserIds { get; set; } = new();
    public List<Guid> NotFoundUserIds { get; set; } = new();
    public string? Message { get; set; }
}

public class AddUsersToTeamCommandHandler : IRequestHandler<AddUsersToTeamCommand, AddUsersToTeamResult>
{
    private readonly ITeamUserRepository _teamUserRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserRepository _userRepository;

    public AddUsersToTeamCommandHandler(
        ITeamUserRepository teamUserRepository,
        ITeamRepository teamRepository,
        IUserRepository userRepository)
    {
        _teamUserRepository = teamUserRepository;
        _teamRepository = teamRepository;
        _userRepository = userRepository;
    }

    public async Task<AddUsersToTeamResult> Handle(AddUsersToTeamCommand request, CancellationToken cancellationToken)
    {
        var result = new AddUsersToTeamResult();

        // Check if team exists and is not deleted
        var team = await _teamRepository.GetByIdAsync(request.TeamId);
        if (team == null || team.IsDeleted)
            throw new EntityNotFoundException("Team not found or is deleted.");

        foreach (var userId in request.UserIds)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                result.NotFoundUserIds.Add(userId);
                continue;
            }

            var existing = await _teamUserRepository.GetByTeamAndUserIdAsync(request.TeamId, userId);
            if (existing == null)
            {
                var teamUser = new TeamUser
                {
                    TeamId = request.TeamId,
                    UserId = userId
                };
                await _teamUserRepository.AddAsync(teamUser);
                result.AddedUserIds.Add(userId);
            }
        }

        if (result.NotFoundUserIds.Count > 0)
        {
            result.Message = $"Added users to team, but the following user IDs do not exist: {string.Join(", ", result.NotFoundUserIds)}";
        }
        else
        {
            result.Message = "All users added to team successfully.";
        }

        return result;
    }
}
