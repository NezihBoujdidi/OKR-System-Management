using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;
using Microsoft.EntityFrameworkCore;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class CollaboratorMonthlyPerformanceService : ICollaboratorMonthlyPerformanceService
{
    private readonly OKRDbContext _context;

    public CollaboratorMonthlyPerformanceService(OKRDbContext context)
    {
        _context = context;
    }

    public async Task<List<CollaboratorMonthlyPerformanceDto>> GetCollaboratorMonthlyPerformanceAsync(Guid organizationId, Guid collaboratorId)
    {
        var now = DateTime.UtcNow;
        var oneYearAgo = now.AddMonths(-11);

        // Ensure all DateTimes are UTC
        var months = Enumerable.Range(0, 12)
            .Select(i => DateTime.SpecifyKind(new DateTime(oneYearAgo.Year, oneYearAgo.Month, 1).AddMonths(i), DateTimeKind.Utc))
            .ToList();

        // Get all tasks for this collaborator in the org, not deleted, with StartedDate in the last 12 months
        var tasks = await _context.Set<KeyResultTask>()
            .Where(t => t.CollaboratorId == collaboratorId
                        && !t.IsDeleted
                        && t.StartedDate >= months.First())
            .ToListAsync();

        var result = new List<CollaboratorMonthlyPerformanceDto>();

        foreach (var monthStart in months)
        {
            var monthEnd = DateTime.SpecifyKind(monthStart.AddMonths(1), DateTimeKind.Utc);

            var collaboratorTasks = tasks
                .Where(t => t.StartedDate >= monthStart && t.StartedDate < monthEnd)
                .ToList();

            double completedScore = 0;
            double assignedScore = 0;
            double overduePenalty = 0;
            var currentDate = DateTime.UtcNow;

            foreach (var task in collaboratorTasks)
            {
                int priorityValue = GetPriorityValue(task.Priority);
                assignedScore += priorityValue;

                int taskStatus = GetStatusValue(task.Status);

                if (taskStatus == (int)Status.Completed)
                {
                    completedScore += priorityValue;
                }
                else if (taskStatus != (int)Status.Completed && task.EndDate.ToUniversalTime() < currentDate)
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

            result.Add(new CollaboratorMonthlyPerformanceDto
            {
                Year = monthStart.Year,
                Month = monthStart.Month,
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
