using System.Threading.Tasks;
using LibraryManagement.Application.DTOs;

namespace LibraryManagement.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task LogoutAsync();
    Task<UserDto?> GetCurrentUserAsync();
    bool IsAuthenticated();

    // update profile information for currently authenticated user
    Task<AuthResponseDto> UpdateUserProfileAsync(UpdateUserDto updateUserDto);
}
