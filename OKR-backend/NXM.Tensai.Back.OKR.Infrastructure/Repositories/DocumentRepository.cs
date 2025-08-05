using Microsoft.EntityFrameworkCore;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;

namespace NXM.Tensai.Back.OKR.Infrastructure.Repositories;

public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    private readonly OKRDbContext _context;

    public DocumentRepository(OKRDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Document?> GetDocumentWithUploaderAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Documents
            .Include(d => d.UploadedBy)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Document>> GetDocumentsBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await _context.Documents
            .Include(d => d.UploadedBy)
            .Where(d => d.OKRSessionId == sessionId)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Document> UploadDocumentAsync(Document document, CancellationToken cancellationToken)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }
} 