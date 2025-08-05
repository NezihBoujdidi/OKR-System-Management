using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.Application.Features.Documents.Interfaces;

namespace NXM.Tensai.Back.OKR.Infrastructure.Services;

public class LocalDocumentStorageService : IDocumentStorageService
{
    private readonly string _storageBasePath;
    private readonly ILogger<LocalDocumentStorageService> _logger;

    public LocalDocumentStorageService(IConfiguration configuration, ILogger<LocalDocumentStorageService> logger)
    {
        _logger = logger;
        
        // Get storage path from configuration, or use default
        _storageBasePath = configuration["LocalStorage:BasePath"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentStorage");
        
        // Ensure directory exists
        if (!Directory.Exists(_storageBasePath))
        {
            Directory.CreateDirectory(_storageBasePath);
        }
        
        _logger.LogInformation("Local document storage initialized at {Path}", _storageBasePath);
    }

    public async Task<string> StoreDocumentAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a unique file name to avoid collisions
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(_storageBasePath, uniqueFileName);
            
            // Create file stream and copy from source
            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);
            }
            
            _logger.LogInformation("Document stored successfully: {FileName}", uniqueFileName);
            
            // Return the relative path (just the filename) as storage path identifier
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
            // Get the full path
            var fullPath = Path.Combine(_storageBasePath, filePath);
            
            // Check if the file exists
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Document not found: {filePath}");
            }
            
            // Create a memory stream to store the file content
            var memoryStream = new MemoryStream();
            
            // Read the file into memory stream
            using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
            }
            
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
            // Get the full path
            var fullPath = Path.Combine(_storageBasePath, filePath);
            
            // Check if the file exists
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Document not found for deletion: {FilePath}", filePath);
                return;
            }
            
            // Delete the file
            File.Delete(fullPath);
            
            _logger.LogInformation("Document deleted successfully: {FilePath}", filePath);
            
            await Task.CompletedTask; // For async compatibility
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: {FilePath}", filePath);
            throw;
        }
    }
} 