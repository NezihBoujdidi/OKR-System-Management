// This file is renamed to DocumentStorageService.cs.bak
// It's kept for reference in case Azure Blob Storage is needed in the future

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.Application.Features.Documents.Interfaces;
// using Azure.Storage.Blobs; - Requires Azure.Storage.Blobs NuGet package
// using Azure.Storage.Blobs.Models; - Requires Azure.Storage.Blobs NuGet package

namespace NXM.Tensai.Back.OKR.Infrastructure.Services;

/* 
public class DocumentStorageService : IDocumentStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<DocumentStorageService> _logger;

    public DocumentStorageService(IConfiguration configuration, ILogger<DocumentStorageService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration["AzureBlobStorage:ConnectionString"];
        var containerName = configuration["AzureBlobStorage:ContainerName"] ?? "documents";
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("Azure Blob Storage connection string is not configured");
        }
        
        // Create a BlobServiceClient
        var blobServiceClient = new BlobServiceClient(connectionString);
        
        // Get a container client
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        
        // Create the container if it doesn't exist
        _containerClient.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<string> StoreDocumentAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a unique file name to avoid collisions
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            
            // Get a blob client
            var blobClient = _containerClient.GetBlobClient(uniqueFileName);
            
            // Set the content type
            var blobUploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            };
            
            // Upload the file
            await blobClient.UploadAsync(fileStream, blobUploadOptions, cancellationToken);
            
            _logger.LogInformation("Document stored successfully: {FileName}", uniqueFileName);
            
            // Return the blob path
            return uniqueFileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing document: {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream> RetrieveDocumentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get a blob client
            var blobClient = _containerClient.GetBlobClient(filePath);
            
            // Check if the blob exists
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Document not found: {filePath}");
            }
            
            // Create a memory stream to store the downloaded blob
            var memoryStream = new MemoryStream();
            
            // Download the blob to the memory stream
            await blobClient.DownloadToAsync(memoryStream, cancellationToken);
            
            // Reset the position to the beginning of the stream
            memoryStream.Position = 0;
            
            _logger.LogInformation("Document retrieved successfully: {FilePath}", filePath);
            
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document: {FilePath}", filePath);
            throw;
        }
    }

    public async Task DeleteDocumentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get a blob client
            var blobClient = _containerClient.GetBlobClient(filePath);
            
            // Check if the blob exists
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Document not found for deletion: {FilePath}", filePath);
                return;
            }
            
            // Delete the blob
            await blobClient.DeleteAsync(cancellationToken: cancellationToken);
            
            _logger.LogInformation("Document deleted successfully: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: {FilePath}", filePath);
            throw;
        }
    }
}
*/ 