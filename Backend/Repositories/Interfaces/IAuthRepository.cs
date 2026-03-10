using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<Role?> GetDefaultRoleAsync();
        Task<User> AddUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
    }
}
