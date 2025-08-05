namespace NXM.Tensai.Back.OKR.Application;

public record GetTeamByIdQuery(Guid Id) : IRequest<TeamDto>;

public class GetTeamByIdQueryValidator : AbstractValidator<GetTeamByIdQuery>
{
    public GetTeamByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Team ID must not be empty.");
    }
}

public class GetTeamByIdQueryHandler : IRequestHandler<GetTeamByIdQuery, TeamDto>
{
    private readonly ITeamRepository _teamRepository;

    public GetTeamByIdQueryHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<TeamDto> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await new GetTeamByIdQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var team = await _teamRepository.GetByIdAsync(request.Id);
        if (team == null || team.IsDeleted)
        {
            throw new NotFoundException(nameof(Team), request.Id);
        }

        return team.ToDto();
    }
}

