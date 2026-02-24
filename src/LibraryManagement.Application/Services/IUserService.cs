using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibraryManagement.Application.DTOs;

namespace LibraryManagement.Application.Services;

public interface IUserService
{
    Task<UserDto?> GetUserProfileAsync(Guid userId);
    Task<UserDto?> UpdateUserProfileAsync(Guid userId, UpdateProfileDto updateDto);
    Task<IEnumerable<BookDto>> GetUserBooksAsync(Guid userId);
    Task<IEnumerable<BookDto>> GetBorrowedBooksAsync(Guid userId);
    Task<bool> DeleteUserAccountAsync(Guid userId);
}
