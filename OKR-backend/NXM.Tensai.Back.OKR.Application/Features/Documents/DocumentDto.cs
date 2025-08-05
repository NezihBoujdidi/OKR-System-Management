using NXM.Tensai.Back.OKR.Domain.Enums;

namespace NXM.Tensai.Back.OKR.Application.Features.Documents;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadDate { get; set; }
    public Guid UploadedById { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public Guid? OKRSessionId { get; set; }
} 