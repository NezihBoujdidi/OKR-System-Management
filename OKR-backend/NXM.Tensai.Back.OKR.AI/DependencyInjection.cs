using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using NXM.Tensai.Back.OKR.AI.Core.AI;
using NXM.Tensai.Back.OKR.AI.Core.AI.Plugins;
using NXM.Tensai.Back.OKR.AI.Services;
using NXM.Tensai.Back.OKR.AI.Services.ChatHistoryService;
using NXM.Tensai.Back.OKR.AI.Services.IntentHandlers;
using NXM.Tensai.Back.OKR.AI.Services.MediatRService;
using NXM.Tensai.Back.OKR.AI.Services.Authorization;
using NXM.Tensai.Back.OKR.AI.Services.Interfaces;

namespace NXM.Tensai.Back.OKR.AI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAIServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register core AI services
            services.AddSingleton<KernelService>();
            services.AddSingleton<PromptTemplateService>();
            services.AddSingleton<IntentSystemMessageGenerator>();
            services.AddSingleton<ChatHistoryManager>();
            services.AddSingleton<EnhancedChatHistory>();
            
            // Register document processing service
            services.AddSingleton<IDocumentProcessingService, DocumentProcessingService>();
            
            // Register DeepSeek Chat Completion Service
            services.AddSingleton<IChatCompletionService>(serviceProvider =>
            {
                var apiKey = configuration["AI:DeepSeek:ApiKey"];
                var model = configuration["AI:DeepSeek:Model"] ?? "deepseek-chat";
                var endpoint = configuration["AI:DeepSeek:Endpoint"] ?? "https://api.deepseek.com/v1/chat/completions";
                var disableDeepThink = configuration.GetValue<bool>("AI:DeepSeek:DisableDeepThink", false);
                var logger = serviceProvider.GetRequiredService<ILogger<DeepSeekChatCompletionService>>();
                
                return new DeepSeekChatCompletionService(apiKey, model, endpoint, disableDeepThink, logger);
            });
            
            // Register the MediatR services - important for direct command/query execution
            services.AddScoped<TeamMediatRService>();
            services.AddScoped<UserMediatRService>();
            services.AddScoped<OkrSessionMediatRService>();
            services.AddScoped<ObjectiveMediatRService>();
            services.AddScoped<KeyResultMediatRService>();
            services.AddScoped<KeyResultTaskMediatRService>();
            services.AddScoped<OKRRiskAnalysisMediatRService>();
            
            // Register function plugins
            services.AddSingleton<OkrSessionPlugin>();
            services.AddSingleton<ObjectivePlugin>();
            services.AddSingleton<TeamPlugin>();
            services.AddSingleton<UserPlugin>();
            services.AddSingleton<KeyResultPlugin>();
            services.AddSingleton<KeyResultTaskPlugin>();
            services.AddSingleton<OKRRiskAnalysisPlugin>();
            
            // Register the new plugin registration service
            services.AddSingleton<PluginRegistrationService>();
              // Register the new Azure OpenAI chat service
            services.AddScoped<AzureOpenAIChatService>();
            
            // Register OKR Analysis Orchestrator Service
            services.AddScoped<OkrAnalysisOrchestratorService>();
            
            // Register intent handlers
            services.AddScoped<IIntentHandler, TeamIntentHandler>();
            services.AddScoped<IIntentHandler, UserIntentHandler>();
            services.AddScoped<IIntentHandler, OkrSessionIntentHandler>();
            services.AddScoped<IIntentHandler, ObjectiveIntentHandler>();
            services.AddScoped<IIntentHandler, KeyResultIntentHandler>();
            services.AddScoped<IIntentHandler, KeyResultTaskIntentHandler>();
            
            // Register new services for clean architecture
            services.AddScoped<IntentProcessor>();
            services.AddScoped<ResponseGenerator>();
            
            // Register vector memory service for conversation context
            services.AddSingleton<VectorContextMemoryService>();            services.AddSingleton<UserContextAccessor>();
            services.AddScoped<PdfGeneratorService>();
            
            // Register OKR Analysis Orchestrator Service
            services.AddScoped<OkrAnalysisOrchestratorService>();
            
            // Register authorization services
            services.AddScoped<IPermissionService, PermissionService>();
            
            return services;
        }
    }
}