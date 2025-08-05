using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NXM.Tensai.Back.OKR.Application.Common.Models;

namespace NXM.Tensai.Back.OKR.Application;

public interface ICollaboratorTaskDetailsService
{
    Task<CollaboratorTaskDetailsDto> GetTaskDetailsAsync(Guid collaboratorId);
}
