namespace NXM.Tensai.Back.OKR.Application;

public record UpdateKeyResultCommandWithId(Guid Id, UpdateKeyResultCommand Command) : IRequest;

public class UpdateKeyResultCommand
{
    public Guid ObjectiveId { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartedDate { get; init; }
    public DateTime EndDate { get; init; }
    public int Progress  { get; set; }
}

public class UpdateKeyResultCommandValidator : AbstractValidator<UpdateKeyResultCommand>
{
    public UpdateKeyResultCommandValidator()
    {
        RuleFor(x => x.ObjectiveId).NotEmpty().WithMessage("Objective ID is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must be at most 100 characters long.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.StartedDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public class UpdateKeyResultCommandHandler : IRequestHandler<UpdateKeyResultCommandWithId>
{
    private readonly IKeyResultRepository _keyResultRepository;
    private readonly IValidator<UpdateKeyResultCommand> _validator;

    public UpdateKeyResultCommandHandler(IKeyResultRepository keyResultRepository, IValidator<UpdateKeyResultCommand> validator)
    {
        _keyResultRepository = keyResultRepository;
        _validator = validator;
    }

    public async Task Handle(UpdateKeyResultCommandWithId request, CancellationToken cancellationToken)
    {
        var (id, command) = request;
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var keyResult = await _keyResultRepository.GetByIdAsync(id);
        if (keyResult == null)
        {
            throw new NotFoundException(nameof(KeyResult), id);
        }

        command.UpdateEntity(keyResult);

        await _keyResultRepository.UpdateAsync(keyResult);
    }
}
