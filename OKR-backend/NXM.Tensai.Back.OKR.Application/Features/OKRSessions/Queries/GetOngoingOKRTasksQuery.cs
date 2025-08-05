using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Application.Common.Models;

namespace NXM.Tensai.Back.OKR.Application.Features.OKRSessions.Queries;

public record GetOngoingOKRTasksQuery(Guid OrganizationId) : IRequest<OngoingOKRTasksResultDto>;

public class GetOngoingOKRTasksQueryHandler : IRequestHandler<GetOngoingOKRTasksQuery, OngoingOKRTasksResultDto>
{
    private readonly IOKRSessionRepository _okrSessionRepository;
    private readonly IObjectiveRepository _objectiveRepository;
    private readonly IKeyResultRepository _keyResultRepository;
    private readonly IKeyResultTaskRepository _keyResultTaskRepository;

    public GetOngoingOKRTasksQueryHandler(
        IOKRSessionRepository okrSessionRepository,
        IObjectiveRepository objectiveRepository,
        IKeyResultRepository keyResultRepository,
        IKeyResultTaskRepository keyResultTaskRepository)
    {
        _okrSessionRepository = okrSessionRepository;
        _objectiveRepository = objectiveRepository;
        _keyResultRepository = keyResultRepository;
        _keyResultTaskRepository = keyResultTaskRepository;
    }

    public async Task<OngoingOKRTasksResultDto> Handle(GetOngoingOKRTasksQuery request, CancellationToken cancellationToken)
    {
        var okrSessions = (await _okrSessionRepository.GetByOrganizationIdAsync(request.OrganizationId))
            .Where(s => s.IsActive && !s.IsDeleted)
            .ToList();

        var result = new OngoingOKRTasksResultDto
        {
            OKRSessions = new List<OngoingOKRSessionDto>()
        };

        foreach (var session in okrSessions)
        {
            var objectives = (await _objectiveRepository.GetBySessionIdAsync(session.Id))
                .Where(o => !o.IsDeleted && ( o.Status != Status.Completed || o.Progress < 100))
                .ToList();

            var objectiveDtos = new List<OngoingObjectiveDto>();

            foreach (var obj in objectives)
            {
                var keyResults = (await _keyResultRepository.GetByObjectiveAsync(obj.Id))
                    .Where(kr => !kr.IsDeleted && (kr.Status != Status.Completed || kr.Progress < 100))
                    .ToList();

                var keyResultDtos = new List<OngoingKeyResultDto>();

                foreach (var kr in keyResults)
                {
                    var tasks = (await _keyResultTaskRepository.GetByKeyResultAsync(kr.Id))
                        .Where(t => !t.IsDeleted && t.Status != Status.Completed)
                        .ToList();

                    var taskDtos = tasks.Select(t => new OngoingTaskDto
                    {
                        Id = t.Id,
                        CollaboratorId = t.CollaboratorId,
                        Title = t.Title,
                        Priority = t.Priority,
                        Status = t.Status,
                        Description = t.Description,
                        StartedDate = t.StartedDate,
                        Progress = t.Progress,
                        EndDate = t.EndDate
                    }).ToList();

                    keyResultDtos.Add(new OngoingKeyResultDto
                    {
                        Id = kr.Id,
                        Title = kr.Title,
                        Description = kr.Description,
                        StartedDate = kr.StartedDate,
                        EndDate = kr.EndDate,
                        Priority = kr.Priority,
                        Status = kr.Status,
                        Progress = kr.Progress,
                        Tasks = taskDtos
                    });
                }

                objectiveDtos.Add(new OngoingObjectiveDto
                {
                    Id = obj.Id,
                    Title = obj.Title,
                    Description = obj.Description,
                    StartedDate = obj.StartedDate,
                    EndDate = obj.EndDate,
                    Priority = obj.Priority,
                    Status = obj.Status,
                    Progress = obj.Progress,
                    TeamId = obj.ResponsibleTeamId,
                    KeyResults = keyResultDtos
                });
            }

            result.OKRSessions.Add(new OngoingOKRSessionDto
            {
                Id = session.Id,
                Title = session.Title,
                Description = session.Description,
                StartedDate = session.StartedDate,
                EndDate = session.EndDate,
                Priority = session.Priority,
                Status = session.Status,
                Progress = session.Progress,
                Objectives = objectiveDtos
            });
        }

        return result;
    }
}
