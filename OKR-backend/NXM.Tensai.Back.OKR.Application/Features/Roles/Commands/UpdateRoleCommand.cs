namespace NXM.Tensai.Back.OKR.Application;

public class UpdateRoleCommand : IRequest
{
    public string OldRoleName { get; init; } = string.Empty;
    public string NewRoleName { get; init; } = string.Empty;
}

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.OldRoleName)
            .NotEmpty().WithMessage("Old role name is required.");

        RuleFor(x => x.NewRoleName)
            .NotEmpty().WithMessage("New role name is required.")
            .MinimumLength(3).WithMessage("New role name must be at least 3 characters long.");
    }
}

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand>
{
    private readonly RoleManager<Role> _roleManager;
    private readonly IValidator<UpdateRoleCommand> _validator;

    public UpdateRoleCommandHandler(RoleManager<Role> roleManager, IValidator<UpdateRoleCommand> validator)
    {
        _roleManager = roleManager;
        _validator = validator;
    }

    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var role = await _roleManager.FindByNameAsync(request.OldRoleName);
        if (role == null)
        {
            throw new KeyNotFoundException("Role not found.");
        }

        role.Name = request.NewRoleName;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            throw new Exception("An error occurred while updating the role.");
        }
    }
}
