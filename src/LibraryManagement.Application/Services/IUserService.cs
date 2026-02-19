using LibraryManagement.Application.DTOs;

namespace LibraryManagement.Application.Services;

public interface IUserService
{
    Task<UserDto?> RegisterAsync(RegisterDto registerDto);
    Task<UserDto?> AuthenticateAsync(LoginDto loginDto);
    Task<UserDto?> GetByIdAsync(Guid id);
}
