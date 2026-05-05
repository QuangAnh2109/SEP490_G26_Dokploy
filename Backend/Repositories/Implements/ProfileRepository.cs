using Backend.Models;
using Backend.Repositories.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly MtcaSep490G26Context _context;

        public ProfileRepository(MtcaSep490G26Context context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}