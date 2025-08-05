namespace NXM.Tensai.Back.OKR.Application;

public record GetObjectivesBySessionIdQuery(Guid OKRSessionId) : IRequest<IEnumerable<ObjectiveDto>>;

public class GetObjectivesBySessionIdQueryValidator : AbstractValidator<GetObjectivesBySessionIdQuery>
{
    public GetObjectivesBySessionIdQueryValidator()
    {
        RuleFor(x => x.OKRSessionId).NotEmpty().WithMessage("Session ID must not be empty.");
    }
}

public class GetObjectivesBySessionIdQueryHandler : IRequestHandler<GetObjectivesBySessionIdQuery, IEnumerable<ObjectiveDto>>
{
    private readonly IObjectiveRepository _objectiveRepository;

    public GetObjectivesBySessionIdQueryHandler(IObjectiveRepository objectiveRepository)
    {
        _objectiveRepository = objectiveRepository;
    }

    public async Task<IEnumerable<ObjectiveDto>> Handle(GetObjectivesBySessionIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await new GetObjectivesBySessionIdQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var objectives = await _objectiveRepository.GetBySessionIdAsync(request.OKRSessionId);
        var filteredObjectives = objectives?.Where(o => !o.IsDeleted).ToList();

        if (filteredObjectives == null || !filteredObjectives.Any())
        {
            throw new NotFoundException(nameof(Objective), request.OKRSessionId);
        }

        return filteredObjectives.Select(o => o.ToDto());
    }
}
