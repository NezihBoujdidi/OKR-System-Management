using MediatR;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;

public class GetCollaboratorsByOrganizationIdQuery : IRequest<List<UserWithRoleDto>>
{
    public Guid OrganizationId { get; set; }
}

public class GetCollaboratorsByOrganizationIdQueryHandler : IRequestHandler<GetCollaboratorsByOrganizationIdQuery, List<UserWithRoleDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<User> _userManager;

    public GetCollaboratorsByOrganizationIdQueryHandler(IUserRepository userRepository, UserManager<User> userManager)
    {
        _userRepository = userRepository;
        _userManager = userManager;
    }

    public async Task<List<UserWithRoleDto>> Handle(GetCollaboratorsByOrganizationIdQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetUsersByOrganizationIdAsync(request.OrganizationId);
        var collaborators = new List<UserWithRoleDto>();

        foreach (var user in users)
        {
            if (await _userManager.IsInRoleAsync(user, "Collaborator"))
            {
                collaborators.Add(user.ToUserWithRoleDto("Collaborator"));
            }
        }

        return collaborators;
    }
}
