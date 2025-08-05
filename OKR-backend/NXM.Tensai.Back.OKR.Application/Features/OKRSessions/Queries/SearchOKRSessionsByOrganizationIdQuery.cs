namespace NXM.Tensai.Back.OKR.Application;

public record SearchOKRSessionsByOrganizationIdQuery(Guid OrganizationId) : IRequest<IEnumerable<OKRSessionDto>>;

public class SearchOKRSessionsByOrganizationIdQueryValidator : AbstractValidator<SearchOKRSessionsByOrganizationIdQuery>
{
    public SearchOKRSessionsByOrganizationIdQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty().WithMessage("Organization ID must not be empty.");
    }
}

public class SearchOKRSessionsByOrganizationIdQueryHandler : IRequestHandler<SearchOKRSessionsByOrganizationIdQuery, IEnumerable<OKRSessionDto>>
{
    private readonly IOKRSessionRepository _okrSessionRepository;

    public SearchOKRSessionsByOrganizationIdQueryHandler(IOKRSessionRepository okrSessionRepository)
    {
        _okrSessionRepository = okrSessionRepository;
    }

    public async Task<IEnumerable<OKRSessionDto>> Handle(SearchOKRSessionsByOrganizationIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await new SearchOKRSessionsByOrganizationIdQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var okrSessions = await _okrSessionRepository.GetByOrganizationIdAsync(request.OrganizationId);
        var filteredSessions = okrSessions.Where(s => !s.IsDeleted).ToList();
        if (!filteredSessions.Any())
        {
            return new List<OKRSessionDto>();
        }

        return filteredSessions.ToDto();
    }
}
