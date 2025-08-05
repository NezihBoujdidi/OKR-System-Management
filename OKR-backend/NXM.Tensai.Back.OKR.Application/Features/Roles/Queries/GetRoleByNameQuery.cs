namespace NXM.Tensai.Back.OKR.Application;

public class GetRoleByNameQuery : IRequest<RoleDto>
{
    public string RoleName { get; init; } = string.Empty;
}

public class GetRoleByNameQueryValidator : AbstractValidator<GetRoleByNameQuery>
{
    public GetRoleByNameQueryValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.");
    }
}

public class GetRoleByNameQueryHandler : IRequestHandler<GetRoleByNameQuery, RoleDto>
{
    private readonly RoleManager<Role> _roleManager;
    private readonly IValidator<GetRoleByNameQuery> _validator;

    public GetRoleByNameQueryHandler(RoleManager<Role> roleManager, IValidator<GetRoleByNameQuery> validator)
    {
        _roleManager = roleManager;
        _validator = validator;
    }

    public async Task<RoleDto> Handle(GetRoleByNameQuery request, CancellationToken cancellationToken)
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

        return new RoleDto { Id = role.Id, Name = role.Name! };
    }
}
