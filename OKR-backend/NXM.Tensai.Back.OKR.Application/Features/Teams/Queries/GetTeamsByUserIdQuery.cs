using NXM.Tensai.Back.OKR.Application;
public record GetTeamsByUserIdQuery(Guid UserId) : IRequest<List<TeamDto>>;
public class GetTeamsByUserIdQueryHandler : IRequestHandler<GetTeamsByUserIdQuery, List<TeamDto>>
{
    private readonly ITeamUserRepository _teamUserRepository;

    public GetTeamsByUserIdQueryHandler(ITeamUserRepository teamUserRepository)
    {
        _teamUserRepository = teamUserRepository;
    }

    public async Task<List<TeamDto>> Handle(GetTeamsByUserIdQuery request, CancellationToken cancellationToken)
    {
        var teams = await _teamUserRepository.GetTeamsByUserIdAsync(request.UserId);
        return teams.Select(t => t.ToDto()).ToList();
    }
}