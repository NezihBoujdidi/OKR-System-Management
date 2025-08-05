namespace NXM.Tensai.Back.OKR.Application;

public record GetKeyResultsByObjectiveIdQuery(Guid ObjectiveId) : IRequest<IEnumerable<KeyResultDto>>;

public class GetKeyResultsByObjectiveIdQueryValidator : AbstractValidator<GetKeyResultsByObjectiveIdQuery>
{
    public GetKeyResultsByObjectiveIdQueryValidator()
    {
        RuleFor(x => x.ObjectiveId).NotEmpty().WithMessage("Objective ID must not be empty.");
    }
}

public class GetKeyResultsByObjectiveIdQueryHandler : IRequestHandler<GetKeyResultsByObjectiveIdQuery, IEnumerable<KeyResultDto>>
{
    private readonly IKeyResultRepository _keyResultRepository;

    public GetKeyResultsByObjectiveIdQueryHandler(IKeyResultRepository keyResultRepository)
    {
        _keyResultRepository = keyResultRepository;
    }

    public async Task<IEnumerable<KeyResultDto>> Handle(GetKeyResultsByObjectiveIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await new GetKeyResultsByObjectiveIdQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var keyResults = await _keyResultRepository.GetByObjectiveAsync(request.ObjectiveId);
        var filteredKeyResults = keyResults?.Where(kr => !kr.IsDeleted).ToList();

        if (filteredKeyResults == null || !filteredKeyResults.Any())
        {
            throw new NotFoundException(nameof(Objective), request.ObjectiveId);
        }

        return filteredKeyResults.Select(o => o.ToDto());
    }
}
