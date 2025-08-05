using Microsoft.AspNetCore.Authorization;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
