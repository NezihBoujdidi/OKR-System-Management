using System.Collections.Generic;
using System.Threading.Tasks;
using NXM.Tensai.Back.OKR.AI.Models;

namespace NXM.Tensai.Back.OKR.AI.Services.IntentHandlers
{
    /// <summary>
    /// Interface for handling specific types of intents
    /// </summary>
    public interface IIntentHandler
    {
        /// <summary>
        /// Determines if this handler can process the given intent
        /// </summary>
        bool CanHandle(string intent);
        
        /// <summary>
        /// Processes the intent with the provided parameters
        /// </summary>
        Task<FunctionExecutionResult> HandleIntentAsync(
            string conversationId,
            string intent,
            Dictionary<string, string> parameters, 
            UserContext userContext);
    }
}
