namespace NXM.Tensai.Back.OKR.Application;

public record GetObjectiveByIdQuery(Guid Id) : IRequest<ObjectiveDto>;

public class GetObjectiveByIdQueryValidator : AbstractValidator<GetObjectiveByIdQuery>
{
    public GetObjectiveByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Objective ID must not be empty.");
    }
}

public class GetObjectiveByIdQueryHandler : IRequestHandler<GetObjectiveByIdQuery, ObjectiveDto>
{
    private readonly IObjectiveRepository _objectiveRepository;

    public GetObjectiveByIdQueryHandler(IObjectiveRepository objectiveRepository)
    {
        _objectiveRepository = objectiveRepository;
    }

    public async Task<ObjectiveDto> Handle(GetObjectiveByIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await new GetObjectiveByIdQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var objective = await _objectiveRepository.GetByIdAsync(request.Id);
        if (objective == null || objective.IsDeleted)
        {
            throw new NotFoundException(nameof(Objective), request.Id);
        }

        return objective.ToDto();
    }
}
