namespace NXM.Tensai.Back.OKR.Application;

public record DeleteTeamCommand(Guid Id) : IRequest;

public class DeleteTeamCommandValidator : AbstractValidator<DeleteTeamCommand>
{
    public DeleteTeamCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Team ID must not be empty.");
    }
}

public class DeleteTeamCommandHandler : IRequestHandler<DeleteTeamCommand>
{
    private readonly ITeamRepository _teamRepository;

    public DeleteTeamCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await new DeleteTeamCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var team = await _teamRepository.GetByIdAsync(request.Id);
        if (team == null)
        {
            throw new NotFoundException(nameof(Team), request.Id);
        }

        // Soft delete: set IsDeleted to true and update
        team.IsDeleted = true;
        await _teamRepository.UpdateAsync(team);
    }
}

