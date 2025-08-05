using System.Security.Claims;

namespace NXM.Tensai.Back.OKR.Application;

public class DeletePermissionFromRoleCommand : IRequest
{
    public string RoleName { get; init; } = string.Empty;
    public string Permission { get; init; } = string.Empty;
}

public class DeletePermissionFromRoleCommandValidator : AbstractValidator<DeletePermissionFromRoleCommand>
{
    public DeletePermissionFromRoleCommandValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.")
            .MinimumLength(3).WithMessage("Role name must be at least 3 characters long.");

        RuleFor(x => x.Permission)
            .NotEmpty().WithMessage("Permission is required.")
            .MinimumLength(3).WithMessage("Permission must be at least 3 characters long.");
    }
}

public class DeletePermissionFromRoleCommandHandler : IRequestHandler<DeletePermissionFromRoleCommand>
{
    private readonly RoleManager<Role> _roleManager;
    private readonly IRoleClaimsRepository _roleClaimsRepository;
    private readonly IValidator<DeletePermissionFromRoleCommand> _validator;

    public DeletePermissionFromRoleCommandHandler(RoleManager<Role> roleManager, IRoleClaimsRepository roleClaimsRepository, IValidator<DeletePermissionFromRoleCommand> validator)
    {
        _roleManager = roleManager;
        _roleClaimsRepository = roleClaimsRepository;
        _validator = validator;
    }

    public async Task Handle(DeletePermissionFromRoleCommand request, CancellationToken cancellationToken)
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

        var hasClaim = await _roleClaimsRepository.HasClaimAsync(role.Id, "Permission", request.Permission);
        if (!hasClaim)
        {
            throw new InvalidOperationException("Permission does not exist for this role.");
        }

        var claim = new Claim("Permission", request.Permission);
        var result = await _roleManager.RemoveClaimAsync(role, claim);
        if (!result.Succeeded)
        {
            throw new Exception("An error occurred while removing the permission.");
        }
    }
}
