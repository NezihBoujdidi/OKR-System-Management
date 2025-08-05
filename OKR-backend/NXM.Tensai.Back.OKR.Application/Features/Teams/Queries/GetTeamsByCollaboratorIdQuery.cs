namespace NXM.Tensai.Back.OKR.Application;

public class GetTeamsByCollaboratorIdQuery : IRequest<IEnumerable<TeamDto>>
{
    public Guid CollaboratorId { get; init; }
}

public class GetTeamsByCollaboratorIdQueryValidator : AbstractValidator<GetTeamsByCollaboratorIdQuery>
{
    public GetTeamsByCollaboratorIdQueryValidator()
    {
        RuleFor(x => x.CollaboratorId)
            .NotEqual(Guid.Empty).WithMessage("Collaborator ID must not be empty.");
    }
}

public class GetTeamsByCollaboratorIdQueryHandler : IRequestHandler<GetTeamsByCollaboratorIdQuery, IEnumerable<TeamDto>>
{
    private readonly ITeamUserRepository _teamUserRepository;
    private readonly IValidator<GetTeamsByCollaboratorIdQuery> _validator;

    public GetTeamsByCollaboratorIdQueryHandler(
        ITeamUserRepository teamUserRepository,
        IValidator<GetTeamsByCollaboratorIdQuery> validator)
    {
        _teamUserRepository = teamUserRepository ?? throw new ArgumentNullException(nameof(teamUserRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<IEnumerable<TeamDto>> Handle(GetTeamsByCollaboratorIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var teams = await _teamUserRepository.GetTeamsByCollaboratorIdAsync(request.CollaboratorId);
        var filteredTeams = teams.Where(t => !t.IsDeleted).ToList();

        if (!filteredTeams.Any())
        {
            throw new EntityNotFoundException($"No teams found for Collaborator ID {request.CollaboratorId}.");
        }

        return filteredTeams.ToDto();
    }
}