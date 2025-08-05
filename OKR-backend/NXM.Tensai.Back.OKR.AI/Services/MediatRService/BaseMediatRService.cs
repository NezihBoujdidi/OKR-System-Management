using System;
using MediatR;
using Microsoft.Extensions.Logging;

namespace NXM.Tensai.Back.OKR.AI.Services.MediatRService
{
    /// <summary>
    /// Base class for all MediatR service implementations
    /// </summary>
    public abstract class BaseMediatRService
    {
        protected readonly IMediator Mediator;
        protected readonly ILogger Logger;

        protected BaseMediatRService(IMediator mediator, ILogger logger)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
    }
}
