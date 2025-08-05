using Microsoft.EntityFrameworkCore;

namespace NXM.Tensai.Back.OKR.Application;

public class GetAllRolesQuery : IRequest<PaginatedListResult<RoleDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetAllRolesQueryValidator : AbstractValidator<GetAllRolesQuery>
{
    public GetAllRolesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1);
    }
}

public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, PaginatedListResult<RoleDto>>
{
    private readonly RoleManager<Role> _roleManager;
    private readonly IValidator<GetAllRolesQuery> _validator;

    public GetAllRolesQueryHandler(RoleManager<Role> roleManager, IValidator<GetAllRolesQuery> validator)
    {
        _roleManager = roleManager;
        _validator = validator;
    }

    public async Task<PaginatedListResult<RoleDto>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var roles = _roleManager.Roles
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var totalRoles = await _roleManager.Roles.CountAsync(cancellationToken);

        var paginatedRoles = new PaginatedListResult<RoleDto>(
            roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name! }).ToList(),
            totalRoles,
            request.Page,
            request.PageSize
        );

        return paginatedRoles;
    }
}
