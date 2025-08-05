using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class CollaboratorTaskDetailsService : ICollaboratorTaskDetailsService
{
    private readonly OKRDbContext _context;

    public CollaboratorTaskDetailsService(OKRDbContext context)
    {
        _context = context;
    }

    public async Task<CollaboratorTaskDetailsDto> GetTaskDetailsAsync(Guid collaboratorId)
    {
        var now = DateTime.UtcNow;

        var tasks = await _context.Set<KeyResultTask>()
            .Where(t => t.CollaboratorId == collaboratorId && !t.IsDeleted)
            .ToListAsync();

        var recentCompleted = tasks
            .Where(t => t.Status == Status.Completed)
            .OrderByDescending(t => t.EndDate)
            .Take(3)
            .ToList();

        var inProgress = tasks
            .Where(t => t.Status == Status.InProgress)
            .ToList();

        var overdue = tasks
            .Where(t => t.Status != Status.Completed && t.EndDate < now)
            .ToList();

        return new CollaboratorTaskDetailsDto
        {
            RecentCompletedTasks = recentCompleted.Select(x => x.ToDto()).ToList(),
            InProgressTasks = inProgress.Select(x => x.ToDto()).ToList(),
            OverdueTasks = overdue.Select(x => x.ToDto()).ToList()
        };
    }
}
