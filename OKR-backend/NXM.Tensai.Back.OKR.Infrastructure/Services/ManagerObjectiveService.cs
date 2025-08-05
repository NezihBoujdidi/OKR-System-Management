using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;


namespace NXM.Tensai.Back.OKR.Infrastructure;

public class ManagerObjectiveService : IManagerObjectiveService
{
    private readonly ITeamRepository _teamRepository;
    private readonly IObjectiveRepository _objectiveRepository;

    public ManagerObjectiveService(ITeamRepository teamRepository, IObjectiveRepository objectiveRepository)
    {
        _teamRepository = teamRepository;
        _objectiveRepository = objectiveRepository;
    }

    public async Task<List<ObjectiveDto>> GetObjectivesByManagerIdAsync(Guid managerId)
    {
        // Get teams managed by the manager
        var teams = await _teamRepository.GetTeamsByManagerIdAsync(managerId);
        var teamIds = teams.Select(t => t.Id).ToList();
        if (!teamIds.Any())
            return new List<ObjectiveDto>();

        // Get all objectives for these teams
        var objectives = new List<Objective>();
        foreach (var teamId in teamIds)
        {
            var teamObjectives = await _objectiveRepository.GetObjectivesByTeamIdAsync(teamId);
            objectives.AddRange(teamObjectives);
        }

        // Filter out deleted objectives
        var filteredObjectives = objectives.Where(o => !o.IsDeleted).ToList();

        // Map to DTOs
        return filteredObjectives.Select(o => o.ToDto()).ToList();
    }
}
