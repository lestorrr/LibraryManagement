using LibraryManagement.Application.DTOs;
using LibraryManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibraryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<FileDto>> UploadFile([FromForm] FileUploadDto uploadDto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var result = await _fileService.UploadFileAsync(uploadDto.File, userId);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FileDto>>> GetUserFiles()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var files = await _fileService.GetUserFilesAsync(userId);
        return Ok(files);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FileDto>> GetFile(Guid id)
    {
        var file = await _fileService.GetFileAsync(id);
        if (file == null)
            return NotFound();
        return Ok(file);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(Guid id)
    {
        try
        {
            var (content, contentType, fileName) = await _fileService.DownloadFileAsync(id);
            return File(content, contentType, fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var result = await _fileService.DeleteFileAsync(id, userId);
        if (!result)
            return NotFound();
        return NoContent();
    }
}