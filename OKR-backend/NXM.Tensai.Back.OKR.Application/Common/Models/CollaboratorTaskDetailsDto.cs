using System;
using System.Collections.Generic;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.Application.Common.Models;

public class CollaboratorTaskDetailsDto
{
    public List<KeyResultTaskDto> RecentCompletedTasks { get; set; }
    public List<KeyResultTaskDto> InProgressTasks { get; set; }
    public List<KeyResultTaskDto> OverdueTasks { get; set; }
}
