using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;
using Microsoft.EntityFrameworkCore;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class CollaboratorTaskStatusStatsService : ICollaboratorTaskStatusStatsService
{
    private readonly OKRDbContext _context;

    public CollaboratorTaskStatusStatsService(OKRDbContext context)
    {
        _context = context;
    }

    public async Task<List<CollaboratorTaskStatusStatsDto>> GetCollaboratorTaskStatusStatsAsync(Guid organizationId)
    {
        // Get all enabled collaborators in the organization
        var collaborators = await _context.Users
            .Where(u => u.OrganizationId == organizationId && u.IsEnabled)
            .ToListAsync();

        var collaboratorIds = collaborators.Select(c => c.Id).ToList();

        // Get all tasks for these collaborators
        var tasks = await _context.Set<KeyResultTask>()
            .Where(t => collaboratorIds.Contains(t.CollaboratorId) && !t.IsDeleted)
            .ToListAsync();

        var result = new List<CollaboratorTaskStatusStatsDto>();

        foreach (var collaborator in collaborators)
        {
            var collaboratorTasks = tasks.Where(t => t.CollaboratorId == collaborator.Id);

            var statusCounts = collaboratorTasks
                .GroupBy(t => t.Status)
                .ToDictionary(
                    g => g.Key ?? Status.NotStarted,
                    g => g.Count()
                );

            result.Add(new CollaboratorTaskStatusStatsDto
            {
                CollaboratorId = collaborator.Id,
                NotStarted = statusCounts.ContainsKey(Status.NotStarted) ? statusCounts[Status.NotStarted] : 0,
                InProgress = statusCounts.ContainsKey(Status.InProgress) ? statusCounts[Status.InProgress] : 0,
                Completed = statusCounts.ContainsKey(Status.Completed) ? statusCounts[Status.Completed] : 0,
                Overdue = statusCounts.ContainsKey(Status.Overdue) ? statusCounts[Status.Overdue] : 0
            });
        }

        return result;
    }
}
