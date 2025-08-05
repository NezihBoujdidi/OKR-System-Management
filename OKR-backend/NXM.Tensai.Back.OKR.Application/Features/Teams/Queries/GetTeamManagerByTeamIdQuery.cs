namespace NXM.Tensai.Back.OKR.Application;
 
public class GetTeamManagerByTeamIdQuery: IRequest<UserDto>
{
    public Guid Id { get; init; }
}
 
public class GetTeamManagerByTeamIdQueryValidator : AbstractValidator<GetTeamManagerByTeamIdQuery>
{
    public GetTeamManagerByTeamIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Team ID must not be empty.");
    }
}
 
public class GetTeamManagerByTeamIdQueryHandler : IRequestHandler<GetTeamManagerByTeamIdQuery, UserDto>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserRepository _userRepository;
 
    public GetTeamManagerByTeamIdQueryHandler(ITeamRepository teamRepository, IUserRepository userRepository)
    {
        _teamRepository = teamRepository;
        _userRepository = userRepository;
    }
 
    public async Task<UserDto> Handle(GetTeamManagerByTeamIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await new GetTeamManagerByTeamIdQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
 
        var team = await _teamRepository.GetByIdAsync(request.Id);
        if (team == null || team.IsDeleted)
        {
            throw new NotFoundException(nameof(Team), request.Id);
        }
        if (team.TeamManagerId == null)
        {
            throw new NotFoundException("Team manager not found for the given team ID.");
        }
        var user = await _userRepository.GetByIdAsync(team.TeamManagerId.Value);
        return user!.ToDto();
    }
}