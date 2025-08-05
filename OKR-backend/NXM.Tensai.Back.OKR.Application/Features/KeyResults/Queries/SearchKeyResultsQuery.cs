namespace NXM.Tensai.Back.OKR.Application;

public class SearchKeyResultsQuery : IRequest<PaginatedListResult<KeyResultDto>>
{
    public string? Title { get; init; }
    public Guid? ObjectiveId { get; init; }
    public Guid? UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class SearchKeyResultsQueryValidator : AbstractValidator<SearchKeyResultsQuery>
{
    public SearchKeyResultsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Title).MaximumLength(100);
    }
}

public class SearchKeyResultsQueryHandler : IRequestHandler<SearchKeyResultsQuery, PaginatedListResult<KeyResultDto>>
{
    private readonly IKeyResultRepository _keyResultRepository;
    private readonly IValidator<SearchKeyResultsQuery> _validator;

    public SearchKeyResultsQueryHandler(IKeyResultRepository keyResultRepository, IValidator<SearchKeyResultsQuery> validator)
    {
        _keyResultRepository = keyResultRepository ?? throw new ArgumentNullException(nameof(keyResultRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<PaginatedListResult<KeyResultDto>> Handle(SearchKeyResultsQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var keyResults = await _keyResultRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            kr =>
                !kr.IsDeleted &&
                (string.IsNullOrEmpty(request.Title) || kr.Title.Contains(request.Title)) &&
                (!request.ObjectiveId.HasValue || kr.ObjectiveId == request.ObjectiveId) &&
                (!request.UserId.HasValue || kr.UserId == request.UserId)
        );

        var paginatedKeyResults = keyResults.ToApplicationPaginatedListResult(k => k.ToDto());

        return paginatedKeyResults;
    }
}
