using LibraryManagement.Application.DTOs;
using LibraryManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace LibraryManagement.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetUserProfileAsync(userId);
            
            if (user == null)
                return NotFound();
            
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            return StatusCode(500, "An error occurred while retrieving profile");
        }
    }

    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var updated = await _userService.UpdateUserProfileAsync(userId, updateDto);
            
            if (updated == null)
                return NotFound();
            
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, "An error occurred while updating profile");
        }
    }

    [HttpGet("books")]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetMyBooks()
    {
        try
        {
            var userId = GetCurrentUserId();
            var books = await _userService.GetUserBooksAsync(userId);
            return Ok(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user books");
            return StatusCode(500, "An error occurred while retrieving your books");
        }
    }

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount()
    {
        try
        {
            var userId = GetCurrentUserId();
            var deleted = await _userService.DeleteUserAccountAsync(userId);
            
            if (!deleted)
                return NotFound();
            
            await HttpContext.SignOutAsync();
            return Ok(new { message = "Account deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user account");
            return StatusCode(500, "An error occurred while deleting account");
        }
    }
}
