using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Domain;
using Microsoft.EntityFrameworkCore;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class CollaboratorPerformanceService : ICollaboratorPerformanceService
{
    private readonly OKRDbContext _context;

    public CollaboratorPerformanceService(OKRDbContext context)
    {
        _context = context;
    }

    public async Task<List<CollaboratorPerformanceDto>> GetCollaboratorPerformanceListAsync(Guid organizationId)
    {
        return await GetCollaboratorPerformanceListAsync(organizationId, null, null);
    }

    public async Task<List<CollaboratorPerformanceRangeDto>> GetCollaboratorPerformanceListWithRangesAsync(Guid organizationId)
    {
        var allTime = await GetCollaboratorPerformanceListAsync(organizationId, null, null);
        var last30Days = await GetCollaboratorPerformanceListAsync(organizationId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        var last3Months = await GetCollaboratorPerformanceListAsync(organizationId, DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow);

        var result = new List<CollaboratorPerformanceRangeDto>(); 
        foreach (var collab in allTime)
        {
            var last30 = last30Days.FirstOrDefault(x => x.CollaboratorId == collab.CollaboratorId)?.Performance ?? 0;
            var last3 = last3Months.FirstOrDefault(x => x.CollaboratorId == collab.CollaboratorId)?.Performance ?? 0;
            result.Add(new CollaboratorPerformanceRangeDto
            {
                CollaboratorId = collab.CollaboratorId,
                PerformanceAllTime = collab.Performance,
                PerformanceLast30Days = last30,
                PerformanceLast3Months = last3
            });
        }
        return result;
    }

    public async Task<List<CollaboratorPerformanceDto>> GetCollaboratorPerformanceListAsync(Guid organizationId, DateTime? from, DateTime? to)
    {
        var collaborators = await _context.Set<User>()
            .Where(u => u.OrganizationId == organizationId && u.IsEnabled)
            .ToListAsync();

        var collaboratorIds = collaborators.Select(c => c.Id).ToList();

        var tasksQuery = _context.Set<KeyResultTask>()
            .Where(t => collaboratorIds.Contains(t.CollaboratorId) && !t.IsDeleted);

        if (from.HasValue)
            tasksQuery = tasksQuery.Where(t => t.StartedDate >= from.Value);
        if (to.HasValue)
            tasksQuery = tasksQuery.Where(t => t.StartedDate <= to.Value);

        // Exclude tasks with a future StartedDate for "all time" (when from and to are null)
        if (!from.HasValue && !to.HasValue)
        {
            var now = DateTime.UtcNow;
            tasksQuery = tasksQuery.Where(t => t.StartedDate <= now);
        }

        var tasks = await tasksQuery.ToListAsync();

        var currentDate = DateTime.UtcNow;
        var result = new List<CollaboratorPerformanceDto>();

        foreach (var collaborator in collaborators)
        {
            var collaboratorTasks = tasks.Where(t => t.CollaboratorId == collaborator.Id).ToList();

            double completedScore = 0;
            double assignedScore = 0;
            double overduePenalty = 0;

            foreach (var task in collaboratorTasks)
            {
                int priorityValue = GetPriorityValue(task.Priority);
                assignedScore += priorityValue;

                int taskStatus = GetStatusValue(task.Status);

                if (taskStatus == (int)Status.Completed)
                {
                    completedScore += priorityValue;
                }
                else if (taskStatus != (int)Status.Completed && task.EndDate < currentDate)
                {
                    overduePenalty += priorityValue * 0.25;
                }
            }

            double performance = 0;
            if (assignedScore + overduePenalty > 0)
            {
                performance = completedScore / (assignedScore + overduePenalty);
            }
            int performancePercentage = (int)Math.Round(performance * 100);

            result.Add(new CollaboratorPerformanceDto
            {
                CollaboratorId = collaborator.Id,
                Performance = performancePercentage
            });
        }

        return result;
    }

    private int GetPriorityValue(Priority? priority)
    {
        return priority switch
        {
            Priority.Urgent => 4,
            Priority.High => 3,
            Priority.Medium => 2,
            Priority.Low => 1,
            _ => 1
        };
    }

    private int GetStatusValue(Status? status)
    {
        return status.HasValue ? (int)status.Value : 0;
    }
}
