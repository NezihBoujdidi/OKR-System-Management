namespace NXM.Tensai.Back.OKR.Application;

public record EnableUserByIdCommand(Guid UserId) : IRequest<UserDto>;

public class EnableUserByIdCommandValidator : AbstractValidator<EnableUserByIdCommand>
{
    public EnableUserByIdCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
    }
}

public class EnableUserByIdCommandHandler : IRequestHandler<EnableUserByIdCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<EnableUserByIdCommand> _validator;

    public EnableUserByIdCommandHandler(IUserRepository userRepository, IValidator<EnableUserByIdCommand> validator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<UserDto> Handle(EnableUserByIdCommand request, CancellationToken cancellationToken)
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

        user.IsEnabled = true;
        user.ModifiedDate = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return user.ToDto();
    }
}