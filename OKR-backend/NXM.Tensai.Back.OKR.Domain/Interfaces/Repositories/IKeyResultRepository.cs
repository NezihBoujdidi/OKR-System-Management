using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NXM.Tensai.Back.OKR.Domain
{
    public interface IKeyResultRepository : IRepository<KeyResult>
    {
        Task<IEnumerable<KeyResult>> GetByObjectiveAsync(Guid objectiveId);
        // Add any specific methods for KeyResult if needed
    }
}

