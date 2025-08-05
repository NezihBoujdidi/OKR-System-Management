using System;
using System.Collections.Generic;

namespace NXM.Tensai.Back.OKR.Application.Common.Models;

public class TeamsWithCollaboratorsResultDto
{
    public List<TeamWithCollaboratorsDto> Teams { get; set; }
}

public class TeamWithCollaboratorsDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? TeamManagerId { get; set; }
    public Guid OrganizationId { get; set; }
    public List<UserWithRoleDto> Collaborators { get; set; }
}
