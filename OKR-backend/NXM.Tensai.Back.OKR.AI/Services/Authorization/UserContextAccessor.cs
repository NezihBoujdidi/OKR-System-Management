using NXM.Tensai.Back.OKR.AI.Models;

namespace NXM.Tensai.Back.OKR.AI.Services.Authorization
{
    /// <summary>
    /// Provides access to the current user context within the scope of a request
    /// </summary>
    public class UserContextAccessor
    {
        /// <summary>
        /// Gets or sets the current user context
        /// </summary>
        public UserContext CurrentUserContext { get; set; }
    }
}
