using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.AI.Services.MediatRService;
using NXM.Tensai.Back.OKR.AI.Services.Authorization;

namespace NXM.Tensai.Back.OKR.AI.Core.AI.Plugins
{
    public class OKRRiskAnalysisPlugin
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OKRRiskAnalysisPlugin> _logger;
        private readonly UserContextAccessor _userContextAccessor;

        public OKRRiskAnalysisPlugin(
            IServiceProvider serviceProvider,
            ILogger<OKRRiskAnalysisPlugin> logger,
            UserContextAccessor userContextAccessor)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _userContextAccessor = userContextAccessor;
        }

        /// <summary>
        /// Get all ongoing OKR tasks with full metadata for risk analysis
        /// </summary>
        [KernelFunction]
        [Description("Get all ongoing OKR tasks and their metadata like objectives, key results and key result tasks for risk analysis")]
        public async Task<object> GetOngoingOKRTasksAsync()
        {
            _logger.LogInformation("LLM called GetOngoingOKRTasksAsync plugin method");
            var userContext = _userContextAccessor.CurrentUserContext;
            if (string.IsNullOrEmpty(userContext?.OrganizationId))
                throw new ArgumentException("OrganizationId is required in user context.");

            using var scope = _serviceProvider.CreateScope();
            var okrRiskAnalysisMediatRService = scope.ServiceProvider.GetRequiredService<OKRRiskAnalysisMediatRService>();
            return await okrRiskAnalysisMediatRService.GetOngoingOKRTasksAsync(userContext.OrganizationId);
        }

        /// <summary>
        /// Get all teams in organization and users of each team in detail for risk analysis
        /// </summary>
        [KernelFunction]
        [Description("Get all teams in organization and users of each team in detail for risk analysis")]
        public async Task<object> GetTeamsWithCollaboratorsAsync()
        {
            _logger.LogInformation("LLM called GetTeamsWithCollaboratorsAsync plugin method");
            var userContext = _userContextAccessor.CurrentUserContext;
            if (string.IsNullOrEmpty(userContext?.OrganizationId))
                throw new ArgumentException("OrganizationId is required in user context.");

            using var scope = _serviceProvider.CreateScope();
            var okrRiskAnalysisMediatRService = scope.ServiceProvider.GetRequiredService<OKRRiskAnalysisMediatRService>();
            return await okrRiskAnalysisMediatRService.GetTeamsWithCollaboratorsAsync(userContext.OrganizationId);
        }
    }
}
