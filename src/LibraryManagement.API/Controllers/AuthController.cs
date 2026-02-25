using LibraryManagement.Application.DTOs;
using LibraryManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto? registerDto)
    {
        if (registerDto == null)
        {
            _logger.LogWarning("Registration request contained no body");
            return BadRequest("Request body is required");
        }

        try
        {
            _logger.LogInformation("Registration attempt for username: {Username}", registerDto.Username);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for registration");
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(registerDto);
            
            if (!result.Success)
            {
                _logger.LogWarning("Registration failed: {Message}", result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Registration successful for username: {Username}", registerDto.Username);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for username: {Username}", registerDto?.Username);
            return StatusCode(500, new AuthResponseDto 
            { 
                Success = false, 
                Message = "An error occurred during registration: " + ex.Message
            });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto? loginDto)
    {
        if (loginDto == null)
        {
            _logger.LogWarning("Login request contained no body");
            return BadRequest("Request body is required");
        }

        try
        {
            _logger.LogInformation("Login attempt for: {UsernameOrEmail}", loginDto.UsernameOrEmail);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for login");
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(loginDto);
            
            if (!result.Success)
            {
                _logger.LogWarning("Login failed for {UsernameOrEmail}: {Message}", loginDto.UsernameOrEmail, result.Message);
                return Unauthorized(result);
            }

            _logger.LogInformation("Login successful for: {UsernameOrEmail}", loginDto.UsernameOrEmail);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for: {UsernameOrEmail}", loginDto?.UsernameOrEmail);
            return StatusCode(500, new AuthResponseDto 
            { 
                Success = false, 
                Message = "An error occurred during login: " + ex.Message
            });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return Ok(new { message = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null)
            return NotFound();
        
        return Ok(user);
    }

    [HttpGet("status")]
    public IActionResult GetAuthStatus()
    {
        return Ok(new { isAuthenticated = _authService.IsAuthenticated() });
    }

    [Authorize]
    [HttpPut("update")]
    public async Task<IActionResult> UpdateUserProfile(UpdateUserDto updateUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.UpdateUserProfileAsync(updateUserDto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, new { Success = false, Message = "An error occurred while updating the profile." });
        }
    }
}
