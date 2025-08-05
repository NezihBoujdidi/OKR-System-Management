using NXM.Tensai.Back.OKR.Application;

public record GetOKRSessionsByTeamIdQuery(Guid TeamId) : IRequest<List<OKRSessionDto>>;

public class GetOKRSessionsByTeamIdQueryHandler : IRequestHandler<GetOKRSessionsByTeamIdQuery, List<OKRSessionDto>>
{
    private readonly IOKRSessionTeamRepository _okrSessionTeamRepository;
    private readonly IOKRSessionRepository _okrSessionRepository;

    public GetOKRSessionsByTeamIdQueryHandler(
        IOKRSessionTeamRepository okrSessionTeamRepository,
        IOKRSessionRepository okrSessionRepository)
    {
        _okrSessionTeamRepository = okrSessionTeamRepository;
        _okrSessionRepository = okrSessionRepository;
    }

    public async Task<List<OKRSessionDto>> Handle(GetOKRSessionsByTeamIdQuery request, CancellationToken cancellationToken)
    {
        var sessionIds = await _okrSessionTeamRepository.GetSessionIdsByTeamIdAsync(request.TeamId);
        if (!sessionIds.Any())
            return new List<OKRSessionDto>();

        var sessions = await _okrSessionRepository.GetByIdsAsync(sessionIds);
        return sessions.Select(s => s.ToDto()).ToList();
    }
}