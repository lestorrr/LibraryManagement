using System.Security.Cryptography;
using AutoMapper;
using LibraryManagement.Application.DTOs;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IRepository<User> _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(IRepository<User> userRepository, IMapper mapper, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<UserDto?> RegisterAsync(RegisterDto registerDto)
    {
        // check uniqueness
        if (await _userRepository.ExistsAsync(u => u.Username == registerDto.Username || u.Email == registerDto.Email))
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = registerDto.Username,
            Email = registerDto.Email,
            FirstName = registerDto.FirstName ?? string.Empty,
            LastName = registerDto.LastName ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = HashPassword(registerDto.Password)
        };

        await _userRepository.AddAsync(user);
        _logger.LogInformation("Registered new user: {Username}", user.Username);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> AuthenticateAsync(LoginDto loginDto)
    {
        var user = (await _userRepository.FindAsync(u => u.Username == loginDto.UsernameOrEmail || u.Email == loginDto.UsernameOrEmail)).FirstOrDefault();
        if (user == null)
        {
            return null;
        }

        if (!VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return null;
        }

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    // --- password hashing (PBKDF2) ---
    private static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] salt = new byte[16];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);

        var combined = new byte[48]; // 16 salt + 32 hash
        Buffer.BlockCopy(salt, 0, combined, 0, 16);
        Buffer.BlockCopy(hash, 0, combined, 16, 32);
        return Convert.ToBase64String(combined);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var combined = Convert.FromBase64String(storedHash);
            var salt = new byte[16];
            Buffer.BlockCopy(combined, 0, salt, 0, 16);
            var hash = new byte[32];
            Buffer.BlockCopy(combined, 16, hash, 0, 32);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(32);

            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }
        catch
        {
            return false;
        }
    }
}
