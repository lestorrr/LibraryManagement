using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using LibraryManagement.Application.DTOs;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<AuthService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            if (await _userRepository.UsernameExistsAsync(registerDto.Username))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Username already exists"
                };
            }

            if (await _userRepository.EmailExistsAsync(registerDto.Email))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Email already exists"
                };
            }

           var user = new User
{
    Id = Guid.NewGuid(), // Or let DB generate: Id = Guid.Empty,
    Username = registerDto.Username,
    Email = registerDto.Email,
    FirstName = registerDto.FirstName,
    LastName = registerDto.LastName,
    PasswordHash = HashPassword(registerDto.Password),
    CreatedAt = DateTime.UtcNow,
    IsActive = true
};

            var createdUser = await _userRepository.AddAsync(user);
            _logger.LogInformation("New user registered: {Username} (PasswordHash length: {HashLen})", user.Username, user.PasswordHash?.Length ?? 0);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Registration successful",
                User = _mapper.Map<UserDto>(createdUser)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Username}", registerDto.Username);
            return new AuthResponseDto
            {
                Success = false,
                Message = "An error occurred during registration"
            };
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        try
        {

            var user = await _userRepository.GetByUsernameOrEmailAsync(loginDto.UsernameOrEmail);

            if (user == null)
            {
                _logger.LogInformation("Login failed: user not found for '{Identifier}'", loginDto.UsernameOrEmail);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid username/email or password"
                };
            }

            if (!VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                _logger.LogInformation("Login failed: password verification failed for user '{Username}' (storedHashLength: {HashLen})", user.Username, user.PasswordHash?.Length ?? 0);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid username/email or password"
                };
            }

            if (!user.IsActive)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Account is deactivated"
                };
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "Cookie");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await _httpContextAccessor.HttpContext!.SignInAsync(claimsPrincipal);

            _logger.LogInformation("User logged in: {Username}", user.Username);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login successful",
                User = _mapper.Map<UserDto>(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user: {Username}", loginDto.UsernameOrEmail);
            return new AuthResponseDto
            {
                Success = false,
                Message = "An error occurred during login"
            };
        }
    }

    public async Task LogoutAsync()
    {
        await _httpContextAccessor.HttpContext!.SignOutAsync();
        _logger.LogInformation("User logged out");
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return null;

        var user = await _userRepository.GetByIdAsync(userId.Value);
        return user != null ? _mapper.Map<UserDto>(user) : null;
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        var hashedInput = HashPassword(password);
        return hashedInput == hash;
    }

    public async Task<AuthResponseDto> UpdateUserProfileAsync(UpdateUserDto updateUserDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return new AuthResponseDto { Success = false, Message = "User not authenticated." };

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
                return new AuthResponseDto { Success = false, Message = "User not found." };

            user.FirstName = updateUserDto.FirstName;
            user.LastName = updateUserDto.LastName;

            await _userRepository.UpdateAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Profile updated successfully.",
                User = _mapper.Map<UserDto>(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return new AuthResponseDto { Success = false, Message = "An error occurred while updating the profile." };
        }
    }
}
