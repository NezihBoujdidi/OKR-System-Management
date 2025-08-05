namespace NXM.Tensai.Back.OKR.Application;

public class GetTeamManagersByOrganizationIdQuery : IRequest<IEnumerable<UserDto>>
{
    public Guid OrganizationId { get; init; }
}

public class GetTeamManagersByOrganizationIdQueryValidator : AbstractValidator<GetTeamManagersByOrganizationIdQuery>
{
    public GetTeamManagersByOrganizationIdQueryValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEqual(Guid.Empty).WithMessage("Organization ID must not be empty.");
    }
}

public class GetTeamManagersByOrganizationIdQueryHandler : IRequestHandler<GetTeamManagersByOrganizationIdQuery, IEnumerable<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<GetTeamManagersByOrganizationIdQuery> _validator;

    public GetTeamManagersByOrganizationIdQueryHandler(
        IUserRepository userRepository,
        IValidator<GetTeamManagersByOrganizationIdQuery> validator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<IEnumerable<UserDto>> Handle(GetTeamManagersByOrganizationIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var teamManagers = await _userRepository.GetTeamManagersByOrganizationIdAsync(request.OrganizationId);
        
        if (!teamManagers.Any())
        {
            throw new EntityNotFoundException($"No team managers found for Organization ID {request.OrganizationId}.");
        }

        return teamManagers.ToDto();
    }
}