namespace NXM.Tensai.Back.OKR.Application;

public record DeleteOrganizationCommand(Guid Id) : IRequest;

public class DeleteOrganizationCommandValidator : AbstractValidator<DeleteOrganizationCommand>
{
    public DeleteOrganizationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Organization ID must not be empty.");
    }
}

public class DeleteOrganizationCommandHandler : IRequestHandler<DeleteOrganizationCommand>
{
    private readonly IOrganizationRepository _organizationRepository;

    public DeleteOrganizationCommandHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.Id);
        if (organization == null)
        {
            throw new NotFoundException(nameof(Organization), request.Id);
        }

        // Soft delete: set IsDeleted to true and update ModifiedDate
        organization.IsDeleted = true;
        organization.ModifiedDate = DateTime.UtcNow;
        await _organizationRepository.UpdateAsync(organization);
    }
}