namespace NXM.Tensai.Back.OKR.Application;

public class CreateRoleCommand : IRequest
{
    public string RoleName { get; init; } = string.Empty;
}

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.")
            .MinimumLength(3).WithMessage("Role name must be at least 3 characters long.");
    }
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand>
{
    private readonly RoleManager<Role> _roleManager;
    private readonly IValidator<CreateRoleCommand> _validator;

    public CreateRoleCommandHandler(RoleManager<Role> roleManager, IValidator<CreateRoleCommand> validator)
    {
        _roleManager = roleManager;
        _validator = validator;
    }

    public async Task Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var roleExists = await _roleManager.RoleExistsAsync(request.RoleName);
        if (roleExists)
        {
            throw new InvalidOperationException("Role already exists.");
        }

        var result = await _roleManager.CreateAsync(new Role { Name = request.RoleName });
        if (!result.Succeeded)
        {
            throw new Exception("An error occurred while creating the role.");
        }
    }
}
