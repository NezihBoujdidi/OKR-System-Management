namespace NXM.Tensai.Back.OKR.Application;

public record UpdateOrganizationCommandWithId(Guid Id, UpdateOrganizationCommand Command) : IRequest;

public class UpdateOrganizationCommand
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Country { get; init; }
    public string? Industry { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public int? Size { get; init; } 
    public bool? IsActive { get; init; } = null;
}

public class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organization name is required.")
            .MaximumLength(100).WithMessage("Organization name must be at most 100 characters long.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.Industry).MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress();
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Size).GreaterThan(0).When(x => x.Size.HasValue).WithMessage("Size must be greater than 0.");
    }
}

public class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommandWithId>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IValidator<UpdateOrganizationCommand> _validator;

    public UpdateOrganizationCommandHandler(IOrganizationRepository organizationRepository, IValidator<UpdateOrganizationCommand> validator)
    {
        _organizationRepository = organizationRepository;
        _validator = validator;
    }

    public async Task Handle(UpdateOrganizationCommandWithId request, CancellationToken cancellationToken)
    {
        var (id, command) = request;
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var organization = await _organizationRepository.GetByIdAsync(id);
        if (organization == null)
        {
            throw new NotFoundException(nameof(Organization), id);
        }

        command.UpdateEntity(organization);

        await _organizationRepository.UpdateAsync(organization);
    }
}
