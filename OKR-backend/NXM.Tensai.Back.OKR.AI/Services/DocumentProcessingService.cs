using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;
using System.Linq;

namespace NXM.Tensai.Back.OKR.AI.Services
{
    /// <summary>
    /// Implementation of the document processing service
    /// </summary>
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly ILogger<DocumentProcessingService> _logger;
        private readonly HashSet<string> _supportedTypes;

        public DocumentProcessingService(ILogger<DocumentProcessingService> logger)
        {
            _logger = logger;
            _supportedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "application/pdf"
                // Add other supported types here as needed
            };
        }

        /// <inheritdoc/>
        public bool IsSupportedFileType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType) && _supportedTypes.Contains(contentType);
        }

        /// <inheritdoc/>
        public async Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
        {
            try
            {
                // Reset stream position
                pdfStream.Position = 0;
                
                // Create a StringBuilder to store the extracted text
                var extractedText = new StringBuilder();
                
                // Variable for page count (moved outside using block)
                int numberOfPages = 0;
                
                // Use iText7 to extract text from the PDF
                using (var reader = new iText.Kernel.Pdf.PdfReader(pdfStream))
                {
                    using (var pdfDocument = new iText.Kernel.Pdf.PdfDocument(reader))
                    {
                        // Get the number of pages
                        numberOfPages = pdfDocument.GetNumberOfPages();
                        _logger.LogInformation("PDF has {NumberOfPages} pages", numberOfPages);
                        
                        // Create a text extraction strategy
                        var strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy();
                        
                        // Extract text from all pages
                        for (int i = 1; i <= numberOfPages; i++)
                        {
                            var page = pdfDocument.GetPage(i);
                            string pageText = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategy);
                            extractedText.AppendLine($"--- Page {i} ---");
                            extractedText.AppendLine(pageText);
                            extractedText.AppendLine();
                            
                            // Log progress for larger PDFs
                            if (i % 10 == 0 && numberOfPages > 20)
                            {
                                _logger.LogInformation("Processed {CurrentPage} of {TotalPages} pages", i, numberOfPages);
                            }
                        }
                    }
                }
                
                string result = extractedText.ToString();
                _logger.LogInformation("Successfully extracted {TextLength} characters from PDF ({Pages} pages)", 
                    result.Length, numberOfPages);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF");
                return $"Error extracting text: {ex.Message}";
            }
        }

        /// <inheritdoc/>
        public async Task<string> PrepareContentForOpenAIAsync(string content, int maxTokens = 4000)
        {
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Empty content provided for OpenAI preparation");
                return string.Empty;
            }

            try
            {
                _logger.LogInformation("Preparing document content for OpenAI, max tokens: {MaxTokens}", maxTokens);
                
                // First clean up the content to remove extra whitespace and redundant page markers
                string cleanedContent = CleanDocumentContent(content);
                
                // Estimate token count
                int estimatedTokens = EstimateTokenCount(cleanedContent);
                _logger.LogInformation("Estimated token count for document: {TokenCount}", estimatedTokens);
                
                // If the content already fits within the token limit, return it as is
                if (estimatedTokens <= maxTokens)
                {
                    _logger.LogInformation("Document content fits within token limit ({CurrentTokens}/{MaxTokens})", 
                        estimatedTokens, maxTokens);
                    return cleanedContent;
                }
                
                // If it's too large, we need to truncate and summarize
                _logger.LogInformation("Document content exceeds token limit ({CurrentTokens}/{MaxTokens}), optimizing...", 
                    estimatedTokens, maxTokens);
                
                // For now, perform a simple truncation to fit within token limit
                // A more sophisticated approach would be to implement intelligent summarization
                // or retain key sections like headings, executive summary, conclusions, etc.
                
                // Approximate characters per token (typically 4 chars = 1 token in English)
                int charsPerToken = 4;
                int approxMaxChars = maxTokens * charsPerToken;
                
                if (cleanedContent.Length > approxMaxChars)
                {
                    // Simple truncation with a note at the end indicating truncation
                    string truncatedContent = cleanedContent.Substring(0, approxMaxChars - 100);
                    truncatedContent += $"\n\n[Note: This document has been truncated to fit within token limits. The full document is {cleanedContent.Length} characters.]";
                    
                    _logger.LogInformation("Truncated document content from {OriginalLength} to {NewLength} characters", 
                        cleanedContent.Length, truncatedContent.Length);
                    
                    return truncatedContent;
                }
                
                return cleanedContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing document content for OpenAI");
                return content; // Return original content on error
            }
        }
        
        /// <inheritdoc/>
        public async Task<string[]> ChunkDocumentContentAsync(string content, int chunkSize = 1000)
        {
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Empty content provided for chunking");
                return Array.Empty<string>();
            }

            try
            {
                // Clean content first
                string cleanedContent = CleanDocumentContent(content);
                
                // Estimate token count
                int estimatedTokens = EstimateTokenCount(cleanedContent);
                
                // If content is small enough to fit in a single chunk, return it
                if (estimatedTokens <= chunkSize)
                {
                    _logger.LogInformation("Document fits in a single chunk ({TokenCount}/{ChunkSize} tokens)", 
                        estimatedTokens, chunkSize);
                    return new[] { cleanedContent };
                }
                
                _logger.LogInformation("Chunking document of {TokenCount} estimated tokens into chunks of ~{ChunkSize} tokens", 
                    estimatedTokens, chunkSize);
                
                // Split by paragraphs first
                string[] paragraphs = Regex.Split(cleanedContent, @"(\r\n|\r|\n){2,}")
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToArray();
                
                var chunks = new List<string>();
                var currentChunk = new StringBuilder();
                int currentChunkTokens = 0;
                
                foreach (var paragraph in paragraphs)
                {
                    // Estimate tokens for this paragraph
                    int paragraphTokens = EstimateTokenCount(paragraph);
                    
                    // If adding this paragraph would exceed chunk size, start a new chunk
                    if (currentChunkTokens + paragraphTokens > chunkSize && currentChunkTokens > 0)
                    {
                        chunks.Add(currentChunk.ToString());
                        currentChunk.Clear();
                        currentChunkTokens = 0;
                    }
                    
                    // If a single paragraph is larger than chunk size, we need to split it
                    if (paragraphTokens > chunkSize)
                    {
                        // If we have content in the current chunk, add it first
                        if (currentChunkTokens > 0)
                        {
                            chunks.Add(currentChunk.ToString());
                            currentChunk.Clear();
                            currentChunkTokens = 0;
                        }
                        
                        // Split this paragraph by sentences
                        string[] sentences = Regex.Split(paragraph, @"(?<=[.!?])\s+")
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToArray();
                        
                        // Add sentences to chunks
                        foreach (var sentence in sentences)
                        {
                            int sentenceTokens = EstimateTokenCount(sentence);
                            
                            // If this sentence alone exceeds chunk size, we have to break it up
                            if (sentenceTokens > chunkSize)
                            {
                                // Split by words and add as many as will fit
                                string[] words = sentence.Split(' ');
                                var sentenceChunk = new StringBuilder();
                                int sentenceChunkTokens = 0;
                                
                                foreach (var word in words)
                                {
                                    int wordTokens = EstimateTokenCount(word + " ");
                                    
                                    if (sentenceChunkTokens + wordTokens > chunkSize)
                                    {
                                        chunks.Add(sentenceChunk.ToString());
                                        sentenceChunk.Clear();
                                        sentenceChunkTokens = 0;
                                    }
                                    
                                    sentenceChunk.Append(word).Append(' ');
                                    sentenceChunkTokens += wordTokens;
                                }
                                
                                if (sentenceChunk.Length > 0)
                                {
                                    chunks.Add(sentenceChunk.ToString());
                                }
                            }
                            else
                            {
                                // If adding this sentence would exceed chunk size, start a new chunk
                                if (currentChunkTokens + sentenceTokens > chunkSize && currentChunkTokens > 0)
                                {
                                    chunks.Add(currentChunk.ToString());
                                    currentChunk.Clear();
                                    currentChunkTokens = 0;
                                }
                                
                                currentChunk.Append(sentence).Append(' ');
                                currentChunkTokens += sentenceTokens;
                            }
                        }
                    }
                    else
                    {
                        // Add paragraph to current chunk
                        currentChunk.AppendLine(paragraph);
                        currentChunk.AppendLine();
                        currentChunkTokens += paragraphTokens;
                    }
                }
                
                // Add the last chunk if it has content
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString());
                }
                
                _logger.LogInformation("Document of {TokenCount} tokens split into {ChunkCount} chunks", 
                    estimatedTokens, chunks.Count);
                
                return chunks.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error chunking document content");
                // Return a single chunk with the original content on error
                return new[] { content };
            }
        }
        
        /// <inheritdoc/>
        public int EstimateTokenCount(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
                
            // Simple token estimation based on GPT tokenization rules
            // Note: This is an approximation! For production, consider using actual tokenizer libraries
            
            // Count words (tokens are often close to word count for English text)
            int wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            
            // Count punctuation separately (each is often a token)
            int punctCount = text.Count(c => ".,:;!?()[]{}\"'".Contains(c));
            
            // Special characters and digits can be separate tokens
            int specialCount = text.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) && !".,:;!?()[]{}\"'".Contains(c));
            
            // Unicode characters outside ASCII range may be multiple tokens
            int unicodeCount = text.Count(c => c > 127);
            
            // Final estimation formula (this is a heuristic and can be adjusted based on performance)
            int estimatedTokens = wordCount + (punctCount / 2) + specialCount + (unicodeCount * 2);
            
            // Add 10% for safety margin
            return (int)(estimatedTokens * 1.1);
        }
        
        /// <summary>
        /// Cleans document content by removing redundant whitespace and normalizing page markers
        /// </summary>
        private string CleanDocumentContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;
                
            // Replace multiple newlines with double newline for paragraph separation
            string cleaned = Regex.Replace(content, @"(\r\n|\r|\n){3,}", "\n\n");
            
            // Normalize page markers to a standard format
            cleaned = Regex.Replace(cleaned, @"---\s*Page\s+\d+\s*---", string.Empty, RegexOptions.IgnoreCase);
            
            // Remove redundant whitespace
            cleaned = Regex.Replace(cleaned, @"[ \t]+", " ");
            
            // Trim leading/trailing whitespace
            cleaned = cleaned.Trim();
            
            return cleaned;
        }
    }
} 