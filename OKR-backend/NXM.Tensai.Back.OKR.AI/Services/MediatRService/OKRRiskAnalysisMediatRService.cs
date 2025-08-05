using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.Application.Features.OKRSessions.Queries;
using NXM.Tensai.Back.OKR.Application.Features.Teams.Queries;
using NXM.Tensai.Back.OKR.Application.Common.Models;

namespace NXM.Tensai.Back.OKR.AI.Services.MediatRService
{
    public class OKRRiskAnalysisMediatRService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<OKRRiskAnalysisMediatRService> _logger;

        public OKRRiskAnalysisMediatRService(IMediator mediator, ILogger<OKRRiskAnalysisMediatRService> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OngoingOKRTasksResultDto> GetOngoingOKRTasksAsync(string organizationId)
        {
            var orgGuid = Guid.Parse(organizationId);
            var query = new GetOngoingOKRTasksQuery(orgGuid);
            return await _mediator.Send(query);
        }

        public async Task<TeamsWithCollaboratorsResultDto> GetTeamsWithCollaboratorsAsync(string organizationId)
        {
            var orgGuid = Guid.Parse(organizationId);
            var query = new GetTeamsWithCollaboratorsQuery(orgGuid);
            return await _mediator.Send(query);
        }
    }
}
