using MediatR;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;

public class MoveMemberFromTeamToTeamCommand : IRequest
{
    public Guid MemberId { get; set; }
    public Guid SourceTeamId { get; set; }
    public Guid NewTeamId { get; set; }
}

public class MoveMemberFromTeamToTeamCommandHandler : IRequestHandler<MoveMemberFromTeamToTeamCommand>
{
    private readonly ITeamUserRepository _teamUserRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IKeyResultTaskRepository _keyResultTaskRepository;

    public MoveMemberFromTeamToTeamCommandHandler(
        ITeamUserRepository teamUserRepository,
        ITeamRepository teamRepository,
        IKeyResultTaskRepository keyResultTaskRepository)
    {
        _teamUserRepository = teamUserRepository;
        _teamRepository = teamRepository;
        _keyResultTaskRepository = keyResultTaskRepository;
    }

    public async Task Handle(MoveMemberFromTeamToTeamCommand request, CancellationToken cancellationToken)
    {
        // Check if source team and new team exist
        var sourceTeam = await _teamRepository.GetByIdAsync(request.SourceTeamId);
        var newTeam = await _teamRepository.GetByIdAsync(request.NewTeamId);
        if (sourceTeam == null || newTeam == null)
            throw new Exception("Source or destination team not found.");

        // Find the TeamUser entry for the member in the source team
        var teamUser = await _teamUserRepository.GetByTeamAndUserIdAsync(request.SourceTeamId, request.MemberId);
        if (teamUser == null)
            throw new Exception("Member not found in the source team.");

        // Check for ongoing KeyResultTasks before removing from source team
        var ongoingTasks = await _keyResultTaskRepository.GetAllAsync();
        bool hasOngoing = ongoingTasks.Any(t =>
            t.CollaboratorId == request.MemberId &&
            t.Status == Status.InProgress &&
            !t.IsDeleted);

        if (hasOngoing)
            throw new UserHasOngoingTaskException();

        // Remove from source team
        await _teamUserRepository.DeleteAsync(teamUser);

        // Add to new team (if not already in)
        var existing = await _teamUserRepository.GetByTeamAndUserIdAsync(request.NewTeamId, request.MemberId);
        if (existing == null)
        {
            var newTeamUser = new TeamUser
            {
                TeamId = request.NewTeamId,
                UserId = request.MemberId
            };
            await _teamUserRepository.AddAsync(newTeamUser);
        }
    }
}
