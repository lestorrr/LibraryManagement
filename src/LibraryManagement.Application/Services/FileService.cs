using AutoMapper;
using LibraryManagement.Application.DTOs;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace LibraryManagement.Application.Services;

public class FileService : IFileService
{
    private readonly IFileRepository _fileRepository;
    private readonly IMapper _mapper;
    private readonly string _uploadPath;

    public FileService(IFileRepository fileRepository, IMapper mapper, IConfiguration configuration)
    {
        _fileRepository = fileRepository;
        _mapper = mapper;
        _uploadPath = configuration["FileStorage:UploadPath"] ?? "uploads";
        
        if (!Directory.Exists(_uploadPath))
            Directory.CreateDirectory(_uploadPath);
    }

    public async Task<FileDto> UploadFileAsync(IFormFile file, Guid userId)
    {
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(_uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileEntity = new FileEntity
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            FilePath = filePath,
            UploadedAt = DateTime.UtcNow,
            UserId = userId
        };

        await _fileRepository.AddAsync(fileEntity);
        return _mapper.Map<FileDto>(fileEntity);
    }

    public async Task<FileDto?> GetFileAsync(Guid fileId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        return file != null ? _mapper.Map<FileDto>(file) : null;
    }

    public async Task<IEnumerable<FileDto>> GetUserFilesAsync(Guid userId)
    {
        var files = await _fileRepository.GetFilesByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<FileDto>>(files);
    }

    public async Task<bool> DeleteFileAsync(Guid fileId, Guid userId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null || file.UserId != userId)
            return false;

        if (File.Exists(file.FilePath))
            File.Delete(file.FilePath);

        await _fileRepository.DeleteAsync(file);
        return true;
    }

    public async Task<(byte[] content, string contentType, string fileName)> DownloadFileAsync(Guid fileId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null || !File.Exists(file.FilePath))
            throw new FileNotFoundException("File not found");

        var content = await File.ReadAllBytesAsync(file.FilePath);
        return (content, file.ContentType, file.OriginalFileName);
    }
}