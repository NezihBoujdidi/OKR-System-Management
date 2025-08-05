namespace NXM.Tensai.Back.OKR.Application;

public class SearchObjectivesQuery : IRequest<PaginatedListResult<ObjectiveDto>>
{
    public string? Title { get; init; }
    public Guid? OKRSessionId { get; init; }
    public Guid? UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class SearchObjectivesQueryValidator : AbstractValidator<SearchObjectivesQuery>
{
    public SearchObjectivesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Title).MaximumLength(100);
    }
}

public class SearchObjectivesQueryHandler : IRequestHandler<SearchObjectivesQuery, PaginatedListResult<ObjectiveDto>>
{
    private readonly IObjectiveRepository _objectiveRepository;
    private readonly IValidator<SearchObjectivesQuery> _validator;

    public SearchObjectivesQueryHandler(IObjectiveRepository objectiveRepository, IValidator<SearchObjectivesQuery> validator)
    {
        _objectiveRepository = objectiveRepository ?? throw new ArgumentNullException(nameof(objectiveRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<PaginatedListResult<ObjectiveDto>> Handle(SearchObjectivesQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var objectives = await _objectiveRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            o =>
                !o.IsDeleted &&
                (string.IsNullOrEmpty(request.Title) || o.Title.Contains(request.Title)) &&
                (!request.OKRSessionId.HasValue || o.OKRSessionId == request.OKRSessionId) &&
                (!request.UserId.HasValue || o.UserId == request.UserId)
        );

        var paginatedObjectives = objectives.ToApplicationPaginatedListResult(o => o.ToDto());

        return paginatedObjectives;
    }
}
