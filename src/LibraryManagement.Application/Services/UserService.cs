using AutoMapper;
using LibraryManagement.Application.DTOs;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IBookRepository bookRepository,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _bookRepository = bookRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<UserDto?> GetUserProfileAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile: {UserId}", userId);
            throw;
        }
    }

    public async Task<UserDto?> UpdateUserProfileAsync(Guid userId, UpdateProfileDto updateDto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return null;

            if (!string.IsNullOrWhiteSpace(updateDto.FirstName))
                user.FirstName = updateDto.FirstName;
            
            if (!string.IsNullOrWhiteSpace(updateDto.LastName))
                user.LastName = updateDto.LastName;
            
            if (!string.IsNullOrWhiteSpace(updateDto.Bio))
                user.Bio = updateDto.Bio;
            
            if (!string.IsNullOrWhiteSpace(updateDto.ProfilePictureUrl))
                user.ProfilePictureUrl = updateDto.ProfilePictureUrl;

            await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("User profile updated: {UserId}", userId);
            
            return _mapper.Map<UserDto>(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<BookDto>> GetUserBooksAsync(Guid userId)
    {
        try
        {
            var books = await _bookRepository.GetUserBooksWithDetailsAsync(userId);
            return _mapper.Map<IEnumerable<BookDto>>(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user books: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<BookDto>> GetBorrowedBooksAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return Enumerable.Empty<BookDto>();

            var books = await _bookRepository.GetBorrowedBooksByMemberEmailAsync(user.Email);
            return _mapper.Map<IEnumerable<BookDto>>(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting borrowed books for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteUserAccountAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.IsActive = false;
            await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("User account deactivated: {UserId}", userId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user account: {UserId}", userId);
            throw;
        }
    }
}
