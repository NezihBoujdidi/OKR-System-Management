namespace NXM.Tensai.Back.OKR.Application;

public class GetTeamsByManagerIdQuery : IRequest<IEnumerable<TeamDto>>
{
    public Guid ManagerId { get; init; }
}

public class GetTeamsByManagerIdQueryValidator : AbstractValidator<GetTeamsByManagerIdQuery>
{
    public GetTeamsByManagerIdQueryValidator()
    {
        RuleFor(x => x.ManagerId)
            .NotEqual(Guid.Empty).WithMessage("Manager ID must not be empty.");
    }
}

public class GetTeamsByManagerIdQueryHandler : IRequestHandler<GetTeamsByManagerIdQuery, IEnumerable<TeamDto>>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IValidator<GetTeamsByManagerIdQuery> _validator;

    public GetTeamsByManagerIdQueryHandler(
        ITeamRepository teamRepository,
        IValidator<GetTeamsByManagerIdQuery> validator)
    {
        _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<IEnumerable<TeamDto>> Handle(GetTeamsByManagerIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var teams = await _teamRepository.GetTeamsByManagerIdAsync(request.ManagerId);
        var filteredTeams = teams.Where(t => !t.IsDeleted).ToList();

        if (!filteredTeams.Any())
        {
            throw new EntityNotFoundException($"No teams found for Manager ID {request.ManagerId}.");
        }

        return filteredTeams.ToDto();
    }
}