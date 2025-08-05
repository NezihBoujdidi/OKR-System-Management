namespace NXM.Tensai.Back.OKR.Domain;

public static class Permissions
{
    public const string AccessAll = "permission:access_all";

    public const string Users_Create = "permission:users_create";
    public const string Users_Update = "permission:users_update";
    public const string Users_GetById = "permission:users_get_by_id";
    public const string Users_GetAll = "permission:users_get_all";
    public const string Users_GetByEmail = "permission:users_get_by_email";
    public const string Users_GetByOrganizationId = "permission:users_get_by_organization_id";
    public const string Users_GetTeamManagersByOrganizationId = "permission:users_get_team_managers_by_organization_id";
    public const string Users_Invite = "permission:users_invite";

    public const string Roles_Create = "permission:roles_create";
    public const string Roles_Update = "permission:roles_update";
    public const string Roles_Delete = "permission:roles_delete";
    public const string Roles_GetById = "permission:roles_get_by_id";
    public const string Roles_GetAll = "permission:roles_get_all";

    public const string UsersRoles_Assign = "permission:usersroles_assign";
    public const string UsersRoles_Remove = "permission:usersroles_remove";
    public const string UsersRoles_GetAll = "permission:usersroles_get_all";

    public const string Organizations_Create = "permission:organizations_create";
    public const string Organizations_Update = "permission:organizations_update";
    public const string Organizations_Delete = "permission:organizations_delete";
    public const string Organizations_GetById = "permission:organizations_get_by_id";
    public const string Organizations_GetAll = "permission:organizations_get_all";

    public const string Objectives_Create = "permission:objectives_create";
    public const string Objectives_Update = "permission:objectives_update";
    public const string Objectives_Delete = "permission:objectives_delete";
    public const string Objectives_GetById = "permission:objectives_get_by_id";
    public const string Objectives_GetAll = "permission:objectives_get_all";

    public const string KeyResults_Create = "permission:keyresults_create";
    public const string KeyResults_Update = "permission:keyresults_update";
    public const string KeyResults_Delete = "permission:keyresults_delete";
    public const string KeyResults_GetById = "permission:keyresults_get_by_id";
    public const string KeyResults_GetAll = "permission:keyresults_get_all";

    public const string KeyResultTasks_Create = "permission:keyresulttasks_create";
    public const string KeyResultTasks_Update = "permission:keyresulttasks_update";
    public const string KeyResultTasks_Delete = "permission:keyresulttasks_delete";
    public const string KeyResultTasks_GetById = "permission:keyresulttasks_get_by_id";
    public const string KeyResultTasks_GetAll = "permission:keyresulttasks_get_all";

    public const string OKRSessions_Create = "permission:okrsessions_create";
    public const string OKRSessions_Update = "permission:okrsessions_update";
    public const string OKRSessions_Delete = "permission:okrsessions_delete";
    public const string OKRSessions_GetById = "permission:okrsessions_get_by_id";
    public const string OKRSessions_GetAll = "permission:okrsessions_get_all";

    public const string Teams_Create = "permission:teams_create";
    public const string Teams_Update = "permission:teams_update";
    public const string Teams_Delete = "permission:teams_delete";
    public const string Teams_GetById = "permission:teams_get_by_id";
    public const string Teams_GetAll = "permission:teams_get_all";
    public const string Teams_GetByOrganizationId = "permission:teams_get_by_organization_id";
    public const string Teams_GetByCollaboratorId = "permission:teams_get_by_collaborator_id";
    public const string Teams_GetByManagerId = "permission:teams_get_by_manager_id";

    public const string Documents_Upload = "permission:documents_upload";
    public const string Documents_Read = "permission:documents_read";
    public const string Documents_Delete = "permission:documents_delete";

    public const string Subscriptions_Create = "permission:subscriptions_create";
    public const string Subscriptions_Update = "permission:subscriptions_update";
    public const string Subscriptions_Cancel = "permission:subscriptions_cancel";
    public const string Subscriptions_View = "permission:subscriptions_view";
}
