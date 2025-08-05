namespace NXM.Tensai.Back.OKR.Application;

public class SearchUserByNameQuery : IRequest<PaginatedListResult<UserDto>>
{
    public string? Query { get; init; }
    public Guid? OrganizationId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class SearchUserByNameQueryValidator : AbstractValidator<SearchUserByNameQuery>
{
    public SearchUserByNameQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Query).MaximumLength(100);
    }
}

public class SearchUserByNameQueryHandler : IRequestHandler<SearchUserByNameQuery, PaginatedListResult<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<SearchUserByNameQuery> _validator;

    public SearchUserByNameQueryHandler(IUserRepository userRepository, IValidator<SearchUserByNameQuery> validator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<PaginatedListResult<UserDto>> Handle(SearchUserByNameQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var users = await _userRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            u => (string.IsNullOrEmpty(request.Query) || 
                 u.FirstName.Contains(request.Query) || 
                 u.LastName.Contains(request.Query)) &&
                 (!request.OrganizationId.HasValue || u.OrganizationId == request.OrganizationId)
        );

        var paginatedUsers = users.ToApplicationPaginatedListResult(u => u.ToDto());

        return paginatedUsers;
    }
}
