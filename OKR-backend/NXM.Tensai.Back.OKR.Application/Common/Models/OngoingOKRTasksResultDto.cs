using System;
using System.Collections.Generic;

namespace NXM.Tensai.Back.OKR.Application.Common.Models;

public class OngoingOKRTasksResultDto
{
    public List<OngoingOKRSessionDto> OKRSessions { get; set; }
}

public class OngoingOKRSessionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartedDate { get; set; }
    public DateTime EndDate { get; set; }
    public Priority? Priority { get; set; }
    public Status? Status { get; set; }
    public int? Progress { get; set; }
    public List<OngoingObjectiveDto> Objectives { get; set; }
}

public class OngoingObjectiveDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public Guid TeamId { get; set; }
    public string Description { get; set; }
    public DateTime StartedDate { get; set; }
    public DateTime EndDate { get; set; }
    public Priority? Priority { get; set; }
    public Status? Status { get; set; }
    public int Progress { get; set; }
    public List<OngoingKeyResultDto> KeyResults { get; set; }
}

public class OngoingKeyResultDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartedDate { get; set; }
    public DateTime EndDate { get; set; }
    public Priority? Priority { get; set; }
    public Status? Status { get; set; }
    public int Progress { get; set; }
    public List<OngoingTaskDto> Tasks { get; set; }
}

public class OngoingTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public Priority? Priority { get; set; }
    public Status? Status { get; set; }
    public int Progress { get; set; }
    public DateTime StartedDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid CollaboratorId { get; set; }
    public string Description { get; set; }
}
