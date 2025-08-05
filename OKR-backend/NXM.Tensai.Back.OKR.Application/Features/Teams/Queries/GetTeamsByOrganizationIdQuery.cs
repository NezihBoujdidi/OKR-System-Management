namespace NXM.Tensai.Back.OKR.Application;

public class GetTeamsByOrganizationIdQuery : IRequest<IEnumerable<TeamDto>>
{
    public Guid OrganizationId { get; init; }
}

public class GetTeamsByOrganizationIdQueryValidator : AbstractValidator<GetTeamsByOrganizationIdQuery>
{
    public GetTeamsByOrganizationIdQueryValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEqual(Guid.Empty).WithMessage("Organization ID must not be empty.");
    }
}

public class GetTeamsByOrganizationIdQueryHandler : IRequestHandler<GetTeamsByOrganizationIdQuery, IEnumerable<TeamDto>>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IValidator<GetTeamsByOrganizationIdQuery> _validator;

    public GetTeamsByOrganizationIdQueryHandler(
        ITeamRepository teamRepository,
        IValidator<GetTeamsByOrganizationIdQuery> validator)
    {
        _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<IEnumerable<TeamDto>> Handle(GetTeamsByOrganizationIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var teams = await _teamRepository.GetTeamsByOrganizationIdAsync(request.OrganizationId);
        var filteredTeams = teams.Where(t => !t.IsDeleted).ToList();

        if (!filteredTeams.Any())
        {
            throw new EntityNotFoundException($"No teams found for Organization ID {request.OrganizationId}.");
        }

        return filteredTeams.ToDto();
    }
}