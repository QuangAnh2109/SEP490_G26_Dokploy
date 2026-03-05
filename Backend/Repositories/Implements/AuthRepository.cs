using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements
{
    public class AuthRepository : IAuthRepository
    {
        private readonly MtcaSep490G26Context _context;

        public AuthRepository(MtcaSep490G26Context context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Role?> GetDefaultRoleAsync()
        {
            // E.g., looking up "User" or "Student"
            return await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User" || r.Name == "Student")
                   ?? await _context.Roles.FirstOrDefaultAsync();
        }

        public async Task<User> AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Reload user reference to get the explicit Role
            if (user.RoleId > 0)
            {
                await _context.Entry(user).Reference(u => u.Role).LoadAsync();
            }

            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}
