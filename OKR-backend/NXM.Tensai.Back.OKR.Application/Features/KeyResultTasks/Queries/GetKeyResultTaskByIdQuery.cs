namespace NXM.Tensai.Back.OKR.Application;

public record GetKeyResultTaskByIdQuery(Guid Id) : IRequest<KeyResultTaskDto>;

public class GetKeyResultTaskByIdQueryValidator : AbstractValidator<GetKeyResultTaskByIdQuery>
{
    public GetKeyResultTaskByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Key Result Task ID must not be empty.");
    }
}

public class GetKeyResultTaskByIdQueryHandler : IRequestHandler<GetKeyResultTaskByIdQuery, KeyResultTaskDto>
{
    private readonly IKeyResultTaskRepository _keyResultTaskRepository;

    public GetKeyResultTaskByIdQueryHandler(IKeyResultTaskRepository keyResultTaskRepository)
    {
        _keyResultTaskRepository = keyResultTaskRepository;
    }

    public async Task<KeyResultTaskDto> Handle(GetKeyResultTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await new GetKeyResultTaskByIdQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var keyResultTask = await _keyResultTaskRepository.GetByIdAsync(request.Id);
        if (keyResultTask == null || keyResultTask.IsDeleted)
        {
            throw new NotFoundException(nameof(KeyResultTask), request.Id);
        }

        return keyResultTask.ToDto();
    }
}
