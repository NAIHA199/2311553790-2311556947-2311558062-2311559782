using LibraryAdvanced.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryAdvanced.Services
{
    public class UserService
    {
        private readonly LibraryAdvancedDbContext _context;

        public UserService(LibraryAdvancedDbContext context)
        {
            _context = context;
        }

        // 1. Tạo tài khoản người dùng mới (Mật khẩu được tự động băm)
        public async Task<User> CreateUserAsync(string username, string rawPassword, string displayName, string? email, int roleId)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

            if (!await _context.Roles.AnyAsync(r => r.Id == roleId))
                throw new InvalidOperationException("RoleId không tồn tại trong hệ thống.");

            var user = new User
            {
                Username = username,
                Password = PasswordHasher.HashPassword(rawPassword), // Lưu chuỗi đã băm vào thuộc tính Password
                DisplayName = displayName,
                Email = email,
                RoleId = roleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // 2. Lấy danh sách phân trang & tìm kiếm
        public async Task<(List<User> Users, int TotalCount)> GetPagedUsersAsync(int page = 1, int pageSize = 10, string? search = null)
        {
            var query = _context.Users.Include(u => u.Role).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Username.Contains(search)
                                      || u.DisplayName.Contains(search)
                                      || (u.Email != null && u.Email.Contains(search)));
            }

            int totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        // 3. Đặt lại mật khẩu người dùng
        public async Task<bool> ResetPasswordAsync(int userId, string newRawPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Password = PasswordHasher.HashPassword(newRawPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // 4. Bật/Tắt trạng thái tài khoản (Khóa hoặc Mở khóa)
        public async Task<bool> ToggleUserStatusAsync(int userId, bool isActive)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}