using System.Security.Claims;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(RoleManager<Role> roleManager)
    {
        var roles = Enum.GetValues<RoleType>();

        foreach (var roleType in roles)
        {
            var roleName = roleType.ToString();
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new Role(roleName);
                await roleManager.CreateAsync(role);
            }

            // Assign permissions to roles
            await AddPermissionsToRole(roleManager, role, roleType);
        }
    }

    private static async Task AddPermissionsToRole(RoleManager<Role> roleManager, Role role, RoleType roleType)
    {
        var permissions = GetPermissionsForRole(roleType);

        foreach (var permission in permissions)
        {
            var claims = await roleManager.GetClaimsAsync(role);
            if (!claims.Any(c => c.Type == "Permission" && c.Value == permission))
            {
                await roleManager.AddClaimAsync(role, new Claim("Permission", permission));
            }
        }
    }

    public static List<string> GetPermissionsForRole(RoleType roleType)
    {
        return roleType switch
        {
            RoleType.SuperAdmin => new List<string>
            {
                Permissions.AccessAll,
                Permissions.Users_Create,
                Permissions.Users_Update,
                Permissions.Users_GetById,
                Permissions.Users_GetAll,
                Permissions.Users_GetByEmail,
                Permissions.Users_GetByOrganizationId,
                Permissions.Users_GetTeamManagersByOrganizationId,
                Permissions.Users_Invite,
                Permissions.Roles_Create,
                Permissions.Roles_Update,
                Permissions.Roles_Delete,
                Permissions.Roles_GetById,
                Permissions.Roles_GetAll,
                Permissions.UsersRoles_Assign,
                Permissions.UsersRoles_Remove,
                Permissions.UsersRoles_GetAll,
                Permissions.Organizations_Create,
                Permissions.Organizations_Update,
                Permissions.Organizations_Delete,
                Permissions.Organizations_GetById,
                Permissions.Organizations_GetAll,
                Permissions.Teams_Create,
                Permissions.Teams_Update,
                Permissions.Teams_Delete,
                Permissions.Teams_GetById,
                Permissions.Teams_GetAll,
                Permissions.Teams_GetByManagerId,
                Permissions.Teams_GetByOrganizationId,
                Permissions.Teams_GetByCollaboratorId,
                Permissions.Objectives_Create,
                Permissions.Objectives_Update,
                Permissions.Objectives_Delete,
                Permissions.Objectives_GetById,
                Permissions.Objectives_GetAll,
                Permissions.KeyResults_Create,
                Permissions.KeyResults_Update,
                Permissions.KeyResults_Delete,
                Permissions.KeyResults_GetById,
                Permissions.KeyResults_GetAll,
                Permissions.KeyResultTasks_Create,
                Permissions.KeyResultTasks_Update,
                Permissions.KeyResultTasks_Delete,
                Permissions.KeyResultTasks_GetById,
                Permissions.KeyResultTasks_GetAll,
                Permissions.OKRSessions_Create,
                Permissions.OKRSessions_Update,
                Permissions.OKRSessions_Delete,
                Permissions.OKRSessions_GetById,
                Permissions.OKRSessions_GetAll,
                Permissions.Subscriptions_Create,
                Permissions.Subscriptions_Update,
                Permissions.Subscriptions_Cancel,
                Permissions.Subscriptions_View
            },
            RoleType.OrganizationAdmin => new List<string>
            {
                Permissions.Users_GetById,
                Permissions.Users_GetAll,
                Permissions.Users_Update,
                Permissions.Users_GetByEmail,
                Permissions.Users_GetByOrganizationId,
                Permissions.Users_GetTeamManagersByOrganizationId,
                Permissions.Users_Invite,
                Permissions.Organizations_Create,
                Permissions.Organizations_Update,
                Permissions.Organizations_GetById,
                Permissions.Organizations_GetAll,
                Permissions.Teams_Create,
                Permissions.Teams_Update,
                Permissions.Teams_Delete,
                Permissions.Teams_GetById,
                Permissions.Teams_GetAll,
                Permissions.Teams_GetByManagerId,
                Permissions.Teams_GetByOrganizationId,
                Permissions.Teams_GetByCollaboratorId,
                Permissions.Objectives_Create,
                Permissions.Objectives_Update,
                Permissions.Objectives_Delete,
                Permissions.Objectives_GetById,
                Permissions.Objectives_GetAll,
                Permissions.KeyResults_Create,
                Permissions.KeyResults_Update,
                Permissions.KeyResults_Delete,
                Permissions.KeyResults_GetById,
                Permissions.KeyResults_GetAll,
                Permissions.KeyResultTasks_Create,
                Permissions.KeyResultTasks_Update,
                Permissions.KeyResultTasks_Delete,
                Permissions.KeyResultTasks_GetById,
                Permissions.KeyResultTasks_GetAll,
                Permissions.OKRSessions_Create,
                Permissions.OKRSessions_Update,
                Permissions.OKRSessions_Delete,
                Permissions.OKRSessions_GetById,
                Permissions.OKRSessions_GetAll,
                Permissions.Subscriptions_Create,
                Permissions.Subscriptions_Update,
                Permissions.Subscriptions_Cancel,
                Permissions.Subscriptions_View
            },
            RoleType.TeamManager => new List<string>
            {
                Permissions.Users_GetById,
                Permissions.Users_GetAll,
                Permissions.Users_GetByEmail,
                Permissions.Users_GetByOrganizationId,
                Permissions.Users_GetTeamManagersByOrganizationId,
                Permissions.Users_Invite,
                Permissions.Teams_GetById,
                Permissions.Teams_GetAll,
                Permissions.Teams_Update,
                Permissions.Teams_GetByManagerId,
                Permissions.Teams_GetByOrganizationId,
                Permissions.Teams_GetByCollaboratorId,
                Permissions.Objectives_Create,
                Permissions.Objectives_Update,
                Permissions.Objectives_Delete,
                Permissions.Objectives_GetById,
                Permissions.Objectives_GetAll,
                Permissions.KeyResults_Create,
                Permissions.KeyResults_Update,
                Permissions.KeyResults_Delete,
                Permissions.KeyResults_GetById,
                Permissions.KeyResults_GetAll,
                Permissions.KeyResultTasks_Create,
                Permissions.KeyResultTasks_Update,
                Permissions.KeyResultTasks_Delete,
                Permissions.KeyResultTasks_GetById,
                Permissions.KeyResultTasks_GetAll,
                Permissions.OKRSessions_GetById,      // Added OKR Session view permissions
                Permissions.OKRSessions_Update,
                Permissions.OKRSessions_GetAll 
            },
            RoleType.Collaborator => new List<string>
            {
                Permissions.Users_GetById,
                Permissions.Users_GetAll,
                Permissions.Users_GetByEmail,
                Permissions.Users_GetByOrganizationId,
                Permissions.Users_GetTeamManagersByOrganizationId,
                Permissions.Teams_GetById,
                Permissions.Teams_GetAll,
                Permissions.Teams_GetByCollaboratorId,
                Permissions.Teams_GetByOrganizationId,
                Permissions.Objectives_GetById,
                Permissions.Objectives_GetAll,
                Permissions.KeyResults_GetById,
                Permissions.KeyResults_GetAll,
                Permissions.KeyResultTasks_Create,
                Permissions.KeyResultTasks_Update,
                Permissions.KeyResultTasks_GetById,
                Permissions.KeyResultTasks_GetAll,
                Permissions.OKRSessions_GetById,      // Added OKR Session view permissions
                Permissions.OKRSessions_GetAll        // Added OKR Session view permissions
            },
            _ => new List<string>()
        };
    }
}
