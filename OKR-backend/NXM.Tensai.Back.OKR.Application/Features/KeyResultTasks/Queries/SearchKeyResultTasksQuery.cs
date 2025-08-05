namespace NXM.Tensai.Back.OKR.Application;

public class SearchKeyResultTasksQuery : IRequest<PaginatedListResult<KeyResultTaskDto>>
{
    public string? Title { get; init; }
    public Guid? KeyResultId { get; init; }
    public Guid? UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class SearchKeyResultTasksQueryValidator : AbstractValidator<SearchKeyResultTasksQuery>
{
    public SearchKeyResultTasksQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Title).MaximumLength(100);
    }
}

public class SearchKeyResultTasksQueryHandler : IRequestHandler<SearchKeyResultTasksQuery, PaginatedListResult<KeyResultTaskDto>>
{
    private readonly IKeyResultTaskRepository _keyResultTaskRepository;
    private readonly IValidator<SearchKeyResultTasksQuery> _validator;

    public SearchKeyResultTasksQueryHandler(IKeyResultTaskRepository keyResultTaskRepository, IValidator<SearchKeyResultTasksQuery> validator)
    {
        _keyResultTaskRepository = keyResultTaskRepository ?? throw new ArgumentNullException(nameof(keyResultTaskRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<PaginatedListResult<KeyResultTaskDto>> Handle(SearchKeyResultTasksQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var keyResultTasks = await _keyResultTaskRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            krt =>
                !krt.IsDeleted &&
                (string.IsNullOrEmpty(request.Title) || krt.Title.Contains(request.Title)) &&
                (!request.KeyResultId.HasValue || krt.KeyResultId == request.KeyResultId) &&
                (!request.UserId.HasValue || krt.UserId == request.UserId)
        );

        var paginatedKeyResultTasks = keyResultTasks.ToApplicationPaginatedListResult(krt => krt.ToDto());

        return paginatedKeyResultTasks;
    }
}
