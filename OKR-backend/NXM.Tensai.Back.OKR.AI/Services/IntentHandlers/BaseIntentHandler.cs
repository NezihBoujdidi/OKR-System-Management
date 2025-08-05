using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Models;

namespace NXM.Tensai.Back.OKR.AI.Services.IntentHandlers
{
    /// <summary>
    /// Base class for all intent handlers
    /// </summary>
    public abstract class BaseIntentHandler : IIntentHandler
    {
        protected readonly ILogger Logger;
        protected readonly KernelService KernelService;
        
        protected BaseIntentHandler(KernelService kernelService, ILogger logger)
        {
            KernelService = kernelService;
            Logger = logger;
        }
        
        public abstract bool CanHandle(string intent);
        
        public abstract Task<FunctionExecutionResult> HandleIntentAsync(
            string conversationId,
            string intent,
            Dictionary<string, string> parameters, 
            UserContext userContext);
            
        /// <summary>
        /// Creates an error result with the specified message
        /// </summary>
        protected FunctionExecutionResult CreateErrorResult(string message)
        {
            return new FunctionExecutionResult
            {
                Success = false,
                Message = message
            };
        }
        
        /// <summary>
        /// Creates a success result with the provided data
        /// </summary>
        protected FunctionExecutionResult CreateSuccessResult<T>(
            T result, 
            string entityType, 
            string entityId, 
            string operation, 
            string message)
        {
            return new FunctionExecutionResult
            {
                Success = true,
                Result = result,
                EntityType = entityType,
                EntityId = entityId,
                Operation = operation,
                Message = message
            };
        }
    }
}
