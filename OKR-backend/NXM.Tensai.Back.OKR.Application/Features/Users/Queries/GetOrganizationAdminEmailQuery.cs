using MediatR;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using NXM.Tensai.Back.OKR.Application;

public class GetOrganizationAdminEmailQuery : IRequest<string>
{
    public Guid OrganizationId { get; set; }
}

public class GetOrganizationAdminEmailQueryHandler : IRequestHandler<GetOrganizationAdminEmailQuery, string>
{
    private readonly IUserRepository _userRepository;

    public GetOrganizationAdminEmailQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<string> Handle(GetOrganizationAdminEmailQuery request, CancellationToken cancellationToken)
    {
        var admin = await _userRepository.GetOrganizationAdminAsync(request.OrganizationId);
        if (admin == null)
        {
            throw new NotFoundException("No admin found for the specified organization.");
        }
        
        return admin.Email;
    }
}