using Microsoft.Extensions.Logging;

namespace NXM.Tensai.Back.OKR.Application;

public class GetUserBySupabaseIdQuery : IRequest<UserWithRoleDto>
{
    public string SupabaseId { get; init; } = null!;
}

public class GetUserBySupabaseIdQueryValidator : AbstractValidator<GetUserBySupabaseIdQuery>
{
    public GetUserBySupabaseIdQueryValidator()
    {
        RuleFor(x => x.SupabaseId)
            .NotEmpty().WithMessage("Supabase ID is required.");
    }
}

public class GetUserBySupabaseIdQueryHandler : IRequestHandler<GetUserBySupabaseIdQuery, UserWithRoleDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<GetUserBySupabaseIdQuery> _validator;
    private readonly UserManager<User> _userManager;

    public GetUserBySupabaseIdQueryHandler(IUserRepository userRepository, IValidator<GetUserBySupabaseIdQuery> validator, UserManager<User> userManager)
    {
        _userRepository = userRepository;
        _validator = validator;
        _userManager = userManager;
    }

    public async Task<UserWithRoleDto> Handle(GetUserBySupabaseIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var user = await _userRepository.GetUserBySupabaseIdAsync(request.SupabaseId);
        if (user == null)
        {
            throw new EntityNotFoundException($"User with Supabase ID {request.SupabaseId} not found.");
        }
        
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        return user.ToUserWithRoleDto(role);
    }
}