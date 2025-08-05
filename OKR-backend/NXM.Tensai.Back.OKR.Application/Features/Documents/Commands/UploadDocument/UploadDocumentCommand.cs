using Microsoft.AspNetCore.Http;
using MediatR;

namespace NXM.Tensai.Back.OKR.Application.Features.Documents.Commands.UploadDocument;

public record UploadDocumentCommand : IRequest<DocumentDto>
{
    public IFormFile File { get; init; } = null!;
    public Guid UserId { get; init; }
    public Guid? OKRSessionId { get; init; }
} 