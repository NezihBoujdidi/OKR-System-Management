namespace NXM.Tensai.Back.OKR.Application;

public record GetOrganizationByIdQuery(Guid Id) : IRequest<OrganizationDto>;

public class GetOrganizationByIdQueryValidator : AbstractValidator<GetOrganizationByIdQuery>
{
    public GetOrganizationByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Organization ID must not be empty.");
    }
}

public class GetOrganizationByIdQueryHandler : IRequestHandler<GetOrganizationByIdQuery, OrganizationDto>
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetOrganizationByIdQueryHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task<OrganizationDto> Handle(GetOrganizationByIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await new GetOrganizationByIdQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var organization = await _organizationRepository.GetByIdAsync(request.Id);
        if (organization == null || organization.IsDeleted)
        {
            throw new NotFoundException(nameof(Organization), request.Id);
        }

        return organization.ToDto();
    }
}
