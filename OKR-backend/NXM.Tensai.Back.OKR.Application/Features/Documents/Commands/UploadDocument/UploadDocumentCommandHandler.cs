using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Application.Features.Documents.Interfaces;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain.Enums;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using MediatR;

namespace NXM.Tensai.Back.OKR.Application.Features.Documents.Commands.UploadDocument;

public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    private readonly IDocumentStorageService _documentStorageService;
    private readonly IDocumentRepository _documentRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UploadDocumentCommandHandler> _logger;

    public UploadDocumentCommandHandler(
        IDocumentStorageService documentStorageService,
        IDocumentRepository documentRepository,
        IUserRepository userRepository,
        ILogger<UploadDocumentCommandHandler> logger)
    {
        _documentStorageService = documentStorageService;
        _documentRepository = documentRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing document upload: {FileName}", request.File.FileName);

        try
        {
            // Validate file
            ValidateFile(request.File);

            using var stream = request.File.OpenReadStream();
            
            // Store file in storage
            var storagePath = await _documentStorageService.StoreDocumentAsync(
                stream, 
                request.File.FileName, 
                request.File.ContentType, 
                cancellationToken);

            // Create document record
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = request.File.FileName,
                ContentType = request.File.ContentType,
                FileSize = request.File.Length,
                UploadDate = DateTime.UtcNow,
                UploadedById = request.UserId,
                StoragePath = storagePath,
                Status = DocumentStatus.Processing,
                OKRSessionId = request.OKRSessionId
            };

            await _documentRepository.UploadDocumentAsync(document, cancellationToken);

            // Get user information
            var user = await _userRepository.GetByIdAsync(request.UserId);
            var userName = user != null ? $"{user.FirstName} {user.LastName}" : string.Empty;

            return new DocumentDto
            {
                Id = document.Id,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSize = document.FileSize,
                UploadDate = document.UploadDate,
                UploadedById = document.UploadedById,
                UploadedByName = userName,
                Status = document.Status,
                OKRSessionId = document.OKRSessionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document upload: {FileName}", request.File.FileName);
            throw;
        }
    }

    private void ValidateFile(IFormFile file)
    {
        // Check if file is empty
        if (file == null || file.Length == 0)
        {
            throw new Application.Common.Exceptions.ValidationException("File is empty");
        }

        // Check file size (limit to 10MB)
        if (file.Length > 10 * 1024 * 1024)
        {
            throw new Application.Common.Exceptions.ValidationException("File size exceeds the limit of 10MB");
        }

        // Check file type (PDF only)
        var allowedTypes = new[] { "application/pdf" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            throw new Application.Common.Exceptions.ValidationException("Only PDF files are allowed");
        }
    }
} 