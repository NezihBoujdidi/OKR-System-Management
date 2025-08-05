using NXM.Tensai.Back.OKR.Domain.Entities;

namespace NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;

public interface IDocumentRepository : IRepository<Document>
{
    Task<Document?> GetDocumentWithUploaderAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Document>> GetDocumentsBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<Document> UploadDocumentAsync(Document document, CancellationToken cancellationToken);
} 