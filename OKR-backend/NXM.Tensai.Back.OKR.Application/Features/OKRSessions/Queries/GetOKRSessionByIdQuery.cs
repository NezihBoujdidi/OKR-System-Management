namespace NXM.Tensai.Back.OKR.Application;

public record GetOKRSessionByIdQuery(Guid Id) : IRequest<OKRSessionDto>;

public class GetOKRSessionByIdQueryValidator : AbstractValidator<GetOKRSessionByIdQuery>
{
    public GetOKRSessionByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("OKR Session ID must not be empty.");
    }
}

public class GetOKRSessionByIdQueryHandler : IRequestHandler<GetOKRSessionByIdQuery, OKRSessionDto>
{
    private readonly IOKRSessionRepository _okrSessionRepository;
    private readonly IOKRSessionTeamRepository _okrSessionTeamRepository;

    public GetOKRSessionByIdQueryHandler(IOKRSessionRepository okrSessionRepository, IOKRSessionTeamRepository okrSessionTeamRepository)
    {
        _okrSessionRepository = okrSessionRepository;
        _okrSessionTeamRepository = okrSessionTeamRepository;
    }

    public async Task<OKRSessionDto> Handle(GetOKRSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await new GetOKRSessionByIdQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var okrSession = await _okrSessionRepository.GetByIdAsync(request.Id);
        if (okrSession == null || okrSession.IsDeleted)
        {
            throw new NotFoundException(nameof(OKRSession), request.Id);
        }

        // Fetch team links for this session
        var teamLinks = await _okrSessionTeamRepository.GetBySessionIdAsync(request.Id);
        var teamIds = teamLinks.Select(x => x.TeamId).ToList();

        var dto = okrSession.ToDto();
        dto.TeamIds = teamIds;
        return dto;
    }
}
