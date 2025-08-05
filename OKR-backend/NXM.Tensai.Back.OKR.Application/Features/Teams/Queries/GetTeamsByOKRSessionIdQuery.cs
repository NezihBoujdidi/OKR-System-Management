namespace NXM.Tensai.Back.OKR.Application;

public class GetTeamsByOKRSessionIdQuery : IRequest<IEnumerable<TeamDto>>
{
    public Guid OKRSessionId { get; set; }
}

public class GetTeamsByOKRSessionIdQueryValidator : AbstractValidator<GetTeamsByOKRSessionIdQuery>
{
    public GetTeamsByOKRSessionIdQueryValidator()
    {
        RuleFor(x => x.OKRSessionId)
            .NotEqual(Guid.Empty).WithMessage("OKR Session ID must not be empty.");
    }
}

public class GetTeamsByOKRSessionIdQueryHandler : IRequestHandler<GetTeamsByOKRSessionIdQuery, IEnumerable<TeamDto>>
{
    private readonly IOKRSessionTeamRepository _okrSessionTeamRepository;
    private readonly ITeamRepository _teamRepository;

    public GetTeamsByOKRSessionIdQueryHandler(
        IOKRSessionTeamRepository okrSessionTeamRepository,
        ITeamRepository teamRepository)
    {
        _okrSessionTeamRepository = okrSessionTeamRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IEnumerable<TeamDto>> Handle(GetTeamsByOKRSessionIdQuery request, CancellationToken cancellationToken)
    {
        // Get all team links for the session
        var links = await _okrSessionTeamRepository.GetBySessionIdAsync(request.OKRSessionId);
        var teamIds = links.Select(x => x.TeamId).ToList();
        if (teamIds.Count == 0)
            return new List<TeamDto>();

        // Fetch all teams in a single call if possible (optimized)
        var teams = await _teamRepository.GetByIdsAsync(teamIds);
        return teams.Select(t => t.ToDto()).ToList();
    }
}