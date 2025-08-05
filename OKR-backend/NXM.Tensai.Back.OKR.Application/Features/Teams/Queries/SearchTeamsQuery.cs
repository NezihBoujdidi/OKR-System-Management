namespace NXM.Tensai.Back.OKR.Application;

public class SearchTeamsQuery : IRequest<PaginatedListResult<TeamDto>>
{
    public string? Name { get; init; }
    public Guid? OrganizationId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class SearchTeamsQueryValidator : AbstractValidator<SearchTeamsQuery>
{
    public SearchTeamsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Name).MaximumLength(100);
    }
}

public class SearchTeamsQueryHandler : IRequestHandler<SearchTeamsQuery, PaginatedListResult<TeamDto>>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IValidator<SearchTeamsQuery> _validator;

    public SearchTeamsQueryHandler(ITeamRepository teamRepository, IValidator<SearchTeamsQuery> validator)
    {
        _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<PaginatedListResult<TeamDto>> Handle(SearchTeamsQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var teams = await _teamRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            t =>
                !t.IsDeleted &&
                (string.IsNullOrEmpty(request.Name) || t.Name.Contains(request.Name)) &&
                (!request.OrganizationId.HasValue || t.OrganizationId == request.OrganizationId)
        );

        var paginatedTeams = teams.ToApplicationPaginatedListResult(t => t.ToDto());

        return paginatedTeams;
    }
}

