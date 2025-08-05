using System;
using System.IO;
using System.Threading.Tasks;

namespace NXM.Tensai.Back.OKR.AI.Services.Interfaces
{
    /// <summary>
    /// Service for processing and extracting content from various document types
    /// </summary>
    public interface IDocumentProcessingService
    {
        /// <summary>
        /// Extracts text from a PDF document
        /// </summary>
        /// <param name="pdfStream">Stream containing the PDF content</param>
        /// <returns>Extracted text content from the PDF</returns>
        Task<string> ExtractTextFromPdfAsync(Stream pdfStream);
        
        /// <summary>
        /// Determines if a file is supported for processing based on its content type
        /// </summary>
        /// <param name="contentType">The MIME content type of the file</param>
        /// <returns>True if the file type is supported, false otherwise</returns>
        bool IsSupportedFileType(string contentType);

        /// <summary>
        /// Prepares document content for OpenAI by optimizing it for token usage
        /// </summary>
        /// <param name="content">The raw document content</param>
        /// <param name="maxTokens">Maximum allowed tokens (approximate)</param>
        /// <returns>Optimized content ready for OpenAI consumption</returns>
        Task<string> PrepareContentForOpenAIAsync(string content, int maxTokens = 4000);
        
        /// <summary>
        /// Chunks large document content for efficient processing by OpenAI
        /// </summary>
        /// <param name="content">The document content to chunk</param>
        /// <param name="chunkSize">Approximate token size for each chunk</param>
        /// <returns>Array of document content chunks</returns>
        Task<string[]> ChunkDocumentContentAsync(string content, int chunkSize = 1000);
        
        /// <summary>
        /// Gets an estimated count of tokens for the given text
        /// </summary>
        /// <param name="text">Text to estimate token count for</param>
        /// <returns>Estimated token count</returns>
        int EstimateTokenCount(string text);
    }
} 