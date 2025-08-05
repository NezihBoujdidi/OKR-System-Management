namespace NXM.Tensai.Back.OKR.Application;

public class DeleteRoleCommand : IRequest
{
    public string RoleName { get; init; } = string.Empty;
}

public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.");
    }
}

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
{
    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly IValidator<DeleteRoleCommand> _validator;

    public DeleteRoleCommandHandler(RoleManager<Role> roleManager, UserManager<User> userManager, IValidator<DeleteRoleCommand> validator)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _validator = validator;
    }

    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var role = await _roleManager.FindByNameAsync(request.RoleName);
        if (role == null)
        {
            throw new KeyNotFoundException("Role not found.");
        }

        // Check if there are any users in the role
        var usersInRole = await _userManager.GetUsersInRoleAsync(request.RoleName);
        if (usersInRole.Any())
        {
            throw new RoleHasUsersException();
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            throw new Exception("An error occurred while deleting the role.");
        }
    }
}
