namespace NXM.Tensai.Back.OKR.Application;

public record DisableUserByIdCommand(Guid UserId) : IRequest<UserDto>;

public class DisableUserByIdCommandValidator : AbstractValidator<DisableUserByIdCommand>
{
    public DisableUserByIdCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
    }
}

public class DisableUserByIdCommandHandler : IRequestHandler<DisableUserByIdCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<DisableUserByIdCommand> _validator;

    public DisableUserByIdCommandHandler(IUserRepository userRepository, IValidator<DisableUserByIdCommand> validator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<UserDto> Handle(DisableUserByIdCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            throw new EntityNotFoundException($"User with ID {request.UserId} not found.");
        }

        user.IsEnabled = false;
        user.ModifiedDate = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return user.ToDto();
    }
}