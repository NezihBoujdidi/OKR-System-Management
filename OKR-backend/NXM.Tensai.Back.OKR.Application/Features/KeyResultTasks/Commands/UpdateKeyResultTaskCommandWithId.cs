namespace NXM.Tensai.Back.OKR.Application;

public record UpdateKeyResultTaskCommandWithId(Guid Id, UpdateKeyResultTaskCommand Command) : IRequest;

public class UpdateKeyResultTaskCommand
{
    public Guid KeyResultId { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartedDate { get; set; }
    public DateTime EndDate { get; init; }
    public int Progress { get; set; }
    public Priority? Priority { get; set; }
    public bool IsDeleted { get; set; }
}

public class UpdateKeyResultTaskCommandValidator : AbstractValidator<UpdateKeyResultTaskCommand>
{
    public UpdateKeyResultTaskCommandValidator()
    {
        RuleFor(x => x.KeyResultId).NotEmpty().WithMessage("Key Result ID is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must be at most 100 characters long.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.EndDate).GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future.");
    }
}

public class UpdateKeyResultTaskCommandHandler : IRequestHandler<UpdateKeyResultTaskCommandWithId>
{
    private readonly IKeyResultTaskRepository _keyResultTaskRepository;
    private readonly IValidator<UpdateKeyResultTaskCommand> _validator;

    public UpdateKeyResultTaskCommandHandler(IKeyResultTaskRepository keyResultTaskRepository, IValidator<UpdateKeyResultTaskCommand> validator)
    {
        _keyResultTaskRepository = keyResultTaskRepository;
        _validator = validator;
    }

    public async Task Handle(UpdateKeyResultTaskCommandWithId request, CancellationToken cancellationToken)
    {
        var (id, command) = request;
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var keyResultTask = await _keyResultTaskRepository.GetByIdAsync(id);
        if (keyResultTask == null)
        {
            throw new NotFoundException(nameof(KeyResultTask), id);
        }

        command.UpdateEntity(keyResultTask);

        await _keyResultTaskRepository.UpdateAsync(keyResultTask);
    }
}
