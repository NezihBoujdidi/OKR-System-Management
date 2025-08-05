namespace NXM.Tensai.Back.OKR.Application;

public record DeleteOKRSessionCommand(Guid Id) : IRequest;

public class DeleteOKRSessionCommandValidator : AbstractValidator<DeleteOKRSessionCommand>
{
    public DeleteOKRSessionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("OKR Session ID must not be empty.");
    }
}

public class DeleteOKRSessionCommandHandler : IRequestHandler<DeleteOKRSessionCommand>
{
    private readonly IOKRSessionRepository _okrSessionRepository;

    public DeleteOKRSessionCommandHandler(IOKRSessionRepository okrSessionRepository)
    {
        _okrSessionRepository = okrSessionRepository;
    }

    public async Task Handle(DeleteOKRSessionCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await new DeleteOKRSessionCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var okrSession = await _okrSessionRepository.GetByIdAsync(request.Id);
        if (okrSession == null)
        {
            throw new NotFoundException(nameof(OKRSession), request.Id);
        }

        // Soft delete: set IsDeleted to true and update ModifiedDate
        okrSession.IsDeleted = true;
        okrSession.ModifiedDate = DateTime.UtcNow;
        await _okrSessionRepository.UpdateAsync(okrSession);
    }
}
