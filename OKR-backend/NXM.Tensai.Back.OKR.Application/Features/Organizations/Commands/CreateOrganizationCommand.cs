namespace NXM.Tensai.Back.OKR.Application;

public class CreateOrganizationCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Country { get; init; }
    public string? Industry { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public int? Size { get; set; }
    public bool IsActive { get; set; } = false;
}

public class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organization name is required.")
            .MaximumLength(100).WithMessage("Organization name must be at most 100 characters long.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.Industry).MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress();
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Size).GreaterThan(0).WithMessage("Number of employees must be greater than 0.")
            .LessThanOrEqualTo(100000).WithMessage("Number of employees cannot exceed 100,000.");
    }
}

public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, Guid>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IValidator<CreateOrganizationCommand> _validator;

    public CreateOrganizationCommandHandler(IOrganizationRepository organizationRepository, IValidator<CreateOrganizationCommand> validator)
    {
        _organizationRepository = organizationRepository;
        _validator = validator;
    }

    public async Task<Guid> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var organization = request.ToEntity();

        await _organizationRepository.AddAsync(organization);
        
        return organization.Id;
    }
}
