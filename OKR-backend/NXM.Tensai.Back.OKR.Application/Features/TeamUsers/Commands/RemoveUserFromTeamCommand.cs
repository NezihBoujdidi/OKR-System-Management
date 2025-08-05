using MediatR;
using FluentValidation;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.Application;

public class RemoveUserFromTeamCommand : IRequest
{
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
}

public class RemoveUserFromTeamCommandValidator : AbstractValidator<RemoveUserFromTeamCommand>
{
    public RemoveUserFromTeamCommandValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class RemoveUserFromTeamCommandHandler : IRequestHandler<RemoveUserFromTeamCommand>
{
    private readonly ITeamUserRepository _teamUserRepository;
    private readonly IValidator<RemoveUserFromTeamCommand> _validator;

    public RemoveUserFromTeamCommandHandler(ITeamUserRepository teamUserRepository, IValidator<RemoveUserFromTeamCommand> validator)
    {
        _teamUserRepository = teamUserRepository;
        _validator = validator;
    }

    public async Task Handle(RemoveUserFromTeamCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var teamUser = await _teamUserRepository.GetByTeamAndUserIdAsync(request.TeamId, request.UserId);

        if (teamUser == null)
            throw new EntityNotFoundException("TeamUser not found.");

        await _teamUserRepository.DeleteAsync(teamUser);
    }
}
