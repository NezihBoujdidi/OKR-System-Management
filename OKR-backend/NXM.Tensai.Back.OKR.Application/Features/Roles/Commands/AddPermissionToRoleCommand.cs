using System.Security.Claims;

namespace NXM.Tensai.Back.OKR.Application;

public class AddPermissionToRoleCommand : IRequest
{
    public string RoleName { get; init; } = string.Empty;
    public string Permission { get; init; } = string.Empty;
}

public class AddPermissionToRoleCommandValidator : AbstractValidator<AddPermissionToRoleCommand>
{
    public AddPermissionToRoleCommandValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.")
            .MinimumLength(3).WithMessage("Role name must be at least 3 characters long.");

        RuleFor(x => x.Permission)
            .NotEmpty().WithMessage("Permission is required.")
            .MinimumLength(3).WithMessage("Permission must be at least 3 characters long.");
    }
}

public class AddPermissionToRoleCommandHandler : IRequestHandler<AddPermissionToRoleCommand>
{
    private readonly RoleManager<Role> _roleManager;
    private readonly IValidator<AddPermissionToRoleCommand> _validator;

    public AddPermissionToRoleCommandHandler(RoleManager<Role> roleManager, IValidator<AddPermissionToRoleCommand> validator)
    {
        _roleManager = roleManager;
        _validator = validator;
    }

    public async Task Handle(AddPermissionToRoleCommand request, CancellationToken cancellationToken)
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

        var claims = await _roleManager.GetClaimsAsync(role);
        if (claims.Any(c => c.Type == "Permission" && c.Value == request.Permission))
        {
            throw new InvalidOperationException("Permission already exists for this role.");
        }

        var result = await _roleManager.AddClaimAsync(role, new Claim("Permission", request.Permission));
        if (!result.Succeeded)
        {
            throw new Exception("An error occurred while adding the permission.");
        }
    }
}
