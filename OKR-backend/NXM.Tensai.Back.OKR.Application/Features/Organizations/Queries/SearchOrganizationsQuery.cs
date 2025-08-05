namespace NXM.Tensai.Back.OKR.Application;

public class SearchOrganizationsQuery : IRequest<PaginatedListResult<OrganizationDto>>
{
    public string? Name { get; init; }
    public string? Country { get; init; }
    public string? Industry { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class SearchOrganizationsQueryValidator : AbstractValidator<SearchOrganizationsQuery>
{
    public SearchOrganizationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Name).MaximumLength(100);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.Industry).MaximumLength(100);
    }
}

public class SearchOrganizationsQueryHandler : IRequestHandler<SearchOrganizationsQuery, PaginatedListResult<OrganizationDto>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IValidator<SearchOrganizationsQuery> _validator;
    private readonly ISubscriptionRepository _subscriptionRepository;

    public SearchOrganizationsQueryHandler(
        IOrganizationRepository organizationRepository,
        IValidator<SearchOrganizationsQuery> validator,
        ISubscriptionRepository subscriptionRepository)
    {
        _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
    }

    public async Task<PaginatedListResult<OrganizationDto>> Handle(SearchOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var organizations = await _organizationRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            o =>
                !o.IsDeleted &&
                (string.IsNullOrEmpty(request.Name) || o.Name.Contains(request.Name)) &&
                (string.IsNullOrEmpty(request.Country) || o.Country == request.Country) &&
                (string.IsNullOrEmpty(request.Industry) || o.Industry == request.Industry)
        );

        // Use .Items to access the list for LINQ
        var orgIds = organizations.Items.Select(o => o.Id).ToList();

        // Fetch all subscriptions and filter in-memory (since GetAllAsync(predicate) is not available)
        var allSubscriptions = await _subscriptionRepository.GetAllAsync();
        var subscriptions = allSubscriptions.Where(s => orgIds.Contains(s.OrganizationId)).ToList();

        var orgIdToPlan = subscriptions
            .GroupBy(s => s.OrganizationId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(s => s.IsActive).ThenByDescending(s => s.EndDate).FirstOrDefault()?.Plan.ToString()
            );

        var paginatedOrganizations = organizations.ToApplicationPaginatedListResult(o =>
            o.ToDto(orgIdToPlan.TryGetValue(o.Id, out var plan) ? plan : null)
        );

        return paginatedOrganizations;
    }
}
