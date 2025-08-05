namespace NXM.Tensai.Back.OKR.Application;

public record UpdateUserCommandWithId : IRequest<UserDto>
{
    public Guid Id { get; }
    public UpdateUserCommand Command { get; }

    public UpdateUserCommandWithId(Guid id, UpdateUserCommand command)
    {
        Id = id;
        Command = command;
    }
}

public class UpdateUserCommand : IRequest<UserDto>
{
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Address { get; init; } = null!;
    public string Position { get; init; } = null!;
    public DateTime DateOfBirth { get; init; }
    public string ProfilePictureUrl { get; init; } = string.Empty;
    public bool IsNotificationEnabled { get; init; }
    public bool IsEnabled { get; init; }
    public Gender Gender { get; init; }
    public Guid? OrganizationId { get; init; }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.DateOfBirth).LessThan(DateTime.UtcNow);
        RuleFor(x => x.Gender).IsInEnum();
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommandWithId, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<UpdateUserCommand> _validator;

    public UpdateUserCommandHandler(IUserRepository userRepository, IValidator<UpdateUserCommand> validator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<UserDto> Handle(UpdateUserCommandWithId request, CancellationToken cancellationToken)
    {
        var command = request.Command;
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var user = await _userRepository.GetByIdAsync(request.Id);
        if (user == null)
        {
            throw new EntityNotFoundException($"User with ID {request.Id} not found.");
        }

        command.UpdateEntity(user);
        await _userRepository.UpdateAsync(user);

        return user.ToDto();
    }
}