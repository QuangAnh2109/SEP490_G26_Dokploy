using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IProfileRepository
    {
        Task<User?> GetUserByIdAsync(int userId);

        Task<User?> GetUserByEmailAsync(string email);

        Task UpdateUserAsync(User user);

        Task SaveChangesAsync();
    }
}