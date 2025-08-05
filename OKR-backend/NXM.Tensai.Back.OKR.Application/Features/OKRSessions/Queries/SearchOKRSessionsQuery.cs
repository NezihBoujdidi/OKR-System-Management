namespace NXM.Tensai.Back.OKR.Application;

public class SearchOKRSessionsQuery : IRequest<PaginatedListResult<OKRSessionDto>>
{
    public string? Title { get; init; }
    public Guid? UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class SearchOKRSessionsQueryValidator : AbstractValidator<SearchOKRSessionsQuery>
{
    public SearchOKRSessionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Title).MaximumLength(100);
    }
}

public class SearchOKRSessionsQueryHandler : IRequestHandler<SearchOKRSessionsQuery, PaginatedListResult<OKRSessionDto>>
{
    private readonly IOKRSessionRepository _okrSessionRepository;
    private readonly IValidator<SearchOKRSessionsQuery> _validator;

    public SearchOKRSessionsQueryHandler(IOKRSessionRepository okrSessionRepository, IValidator<SearchOKRSessionsQuery> validator)
    {
        _okrSessionRepository = okrSessionRepository ?? throw new ArgumentNullException(nameof(okrSessionRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<PaginatedListResult<OKRSessionDto>> Handle(SearchOKRSessionsQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var okrSessions = await _okrSessionRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            o =>
                !o.IsDeleted &&
                (string.IsNullOrEmpty(request.Title) || o.Title.Contains(request.Title)) &&
                (!request.UserId.HasValue || o.UserId == request.UserId)
        );

        var paginatedOKRSessions = okrSessions.ToApplicationPaginatedListResult(okr => okr.ToDto());

        return paginatedOKRSessions;
    }
}
