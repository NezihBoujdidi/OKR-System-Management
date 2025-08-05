namespace NXM.Tensai.Back.OKR.Application;

public class ValidateKeyQuery : IRequest<ValidateKeyDto>
{
    public string Key { get; init; } = string.Empty;
}

public class ValidateKeyQueryValidator : AbstractValidator<ValidateKeyQuery>
{
    public ValidateKeyQueryValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Key is required");
            
    }
}

public class ValidateKeyQueryHandler : IRequestHandler<ValidateKeyQuery, ValidateKeyDto>
{
    
    private readonly IInvitationLinkRepository _invitationLinkRepository;
    private readonly IValidator<ValidateKeyQuery> _validator;

    public ValidateKeyQueryHandler(IInvitationLinkRepository invitationLinkRepository, IValidator<ValidateKeyQuery> validator)
    {
        _invitationLinkRepository = invitationLinkRepository ?? throw new ArgumentNullException(nameof(invitationLinkRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ValidateKeyDto> Handle(ValidateKeyQuery request, CancellationToken cancellationToken)
    {
        // Validate the incoming query
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Retrieve the invitation link from the repository using the provided key (token)
        var invitationLink = await _invitationLinkRepository.GetByTokenAsync(request.Key);

        // Check if the invitation link exists and has not expired
        if (invitationLink == null || invitationLink.ExpirationDate < DateTime.UtcNow)
        {
            throw new ValidationException("Invalid or expired invitation link.");
        }

        // Map the domain model (InvitationLink) to the DTO
        return invitationLink.ToDto();
    }
}

