using System;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NXM.Tensai.Back.OKR.AI.Core.AI.Plugins;

namespace NXM.Tensai.Back.OKR.AI.Services
{
    /// <summary>
    /// Service responsible for registering plugins to kernels
    /// </summary>
    public class PluginRegistrationService
    {
        private readonly ILogger<PluginRegistrationService> _logger;
        private readonly TeamPlugin _teamPlugin;
        private readonly UserPlugin _userPlugin;
        private readonly OkrSessionPlugin _okrSessionPlugin;
        private readonly ObjectivePlugin _objectivePlugin;
        private readonly KeyResultPlugin _keyResultPlugin;
        private readonly KeyResultTaskPlugin _keyResultTaskPlugin;
        private readonly OKRRiskAnalysisPlugin _okrRiskAnalysisPlugin;
        private static bool _pluginsRegistered = false;
        private static readonly object _lockObject = new object();

        public PluginRegistrationService(
            TeamPlugin teamPlugin, 
            UserPlugin userPlugin,
            OkrSessionPlugin okrSessionPlugin,
            ObjectivePlugin objectivePlugin,
            KeyResultPlugin keyResultPlugin,
            KeyResultTaskPlugin keyResultTaskPlugin,
            OKRRiskAnalysisPlugin okrRiskAnalysisPlugin,
            ILogger<PluginRegistrationService> logger)
        {
            _teamPlugin = teamPlugin;
            _userPlugin = userPlugin;
            _okrSessionPlugin = okrSessionPlugin;
            _objectivePlugin = objectivePlugin;
            _keyResultPlugin = keyResultPlugin;
            _keyResultTaskPlugin = keyResultTaskPlugin;
            _okrRiskAnalysisPlugin = okrRiskAnalysisPlugin;
            _logger = logger;
        }

        /// <summary>
        /// Register plugins to the provided kernel
        /// </summary>
        public void RegisterPlugins(Kernel kernel)
        {
            lock (_lockObject)
            {
                try
                {
                    // Register TeamPlugin
                    kernel.Plugins.AddFromObject(_teamPlugin, "TeamManagement");
                    _logger.LogInformation("Successfully registered TeamManagement plugin");
                    
                    // Register UserPlugin
                    kernel.Plugins.AddFromObject(_userPlugin, "UserManagement");
                    _logger.LogInformation("Successfully registered UserManagement plugin");

                    // Register OkrSessionPlugin
                    kernel.Plugins.AddFromObject(_okrSessionPlugin, "OkrSessionManagement");
                    _logger.LogInformation("Successfully registered OkrSessionManagement plugin");

                    // Register ObjectivePlugin
                    kernel.Plugins.AddFromObject(_objectivePlugin, "ObjectiveManagement");
                    _logger.LogInformation("Successfully registered ObjectiveManagement plugin");
                    
                    // Register KeyResultPlugin
                    kernel.Plugins.AddFromObject(_keyResultPlugin, "KeyResultManagement");
                    _logger.LogInformation("Successfully registered KeyResultManagement plugin");
                    
                    // Register KeyResultTaskPlugin
                    kernel.Plugins.AddFromObject(_keyResultTaskPlugin, "KeyResultTaskManagement");
                    _logger.LogInformation("Successfully registered KeyResultTaskManagement plugin");

                    kernel.Plugins.AddFromObject(_okrRiskAnalysisPlugin, "OKRRiskAnalysis");
                    _logger.LogInformation("Successfully registered OKRRiskAnalysis plugin");
                    
                    _pluginsRegistered = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error registering plugins to kernel");
                    throw;
                }
            }
        }
    }
}
