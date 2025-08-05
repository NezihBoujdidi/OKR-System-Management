namespace NXM.Tensai.Back.OKR.Application;

public record GetKeyResultsTasksByKeyResultIdQuery(Guid KeyResultId) : IRequest<IEnumerable<KeyResultTaskDto>>;

public class GetKeyResultsTasksByKeyResultIdQueryValidator : AbstractValidator<GetKeyResultsTasksByKeyResultIdQuery>
{
    public GetKeyResultsTasksByKeyResultIdQueryValidator()
    {
        RuleFor(x => x.KeyResultId).NotEmpty().WithMessage("KeyResult ID must not be empty.");
    }
}

public class GetKeyResultsTasksByKeyResultIdQueryHandler : IRequestHandler<GetKeyResultsTasksByKeyResultIdQuery, IEnumerable<KeyResultTaskDto>>
{
    private readonly IKeyResultTaskRepository _keyResultTaskRepository;

    public GetKeyResultsTasksByKeyResultIdQueryHandler(IKeyResultTaskRepository keyResultTaskRepository)
    {
        _keyResultTaskRepository = keyResultTaskRepository;
    }

    public async Task<IEnumerable<KeyResultTaskDto>> Handle(GetKeyResultsTasksByKeyResultIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await new GetKeyResultsTasksByKeyResultIdQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var keyResultTasks = await _keyResultTaskRepository.GetByKeyResultAsync(request.KeyResultId);
        var filteredTasks = keyResultTasks?.Where(krt => !krt.IsDeleted).ToList();

        if (filteredTasks == null || !filteredTasks.Any())
        {
            throw new NotFoundException(nameof(Objective), request.KeyResultId);
        }

        return filteredTasks.Select(o => o.ToDto());
    }
}
