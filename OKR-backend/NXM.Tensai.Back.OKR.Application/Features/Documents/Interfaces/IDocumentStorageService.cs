namespace NXM.Tensai.Back.OKR.Application.Features.Documents.Interfaces;

public interface IDocumentStorageService
{
    Task<string> StoreDocumentAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> RetrieveDocumentAsync(string filePath, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(string filePath, CancellationToken cancellationToken = default);
} 