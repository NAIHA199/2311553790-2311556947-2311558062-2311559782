using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryAdvanced.Models;
using LibraryAdvanced.Services;

namespace LibraryAdvanced.Controllers
{
    public class UserController : Controller
    {
        private readonly UserService _userService;
        private readonly LibraryAdvancedDbContext _context;

        public UserController(UserService userService, LibraryAdvancedDbContext context)
        {
            _userService = userService;
            _context = context;
        }

        // 1. Trang danh sách người dùng (URL: /User/Index)
        public async Task<IActionResult> Index(int page = 1, string? search = null)
        {
            int pageSize = 8;
            var (users, totalCount) = await _userService.GetPagedUsersAsync(page, pageSize, search);

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return View(users);
        }

        // 2. Trang tạo người dùng (GET: /User/Create)
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "Id", "Name");
            return View();
        }

        // Processing Tạo người dùng (POST: /User/Create)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string username, string password, string displayName, string? email, int roleId)
        {
            try
            {
                await _userService.CreateUserAsync(username, password, displayName, email, roleId);
                TempData["Success"] = "Thêm người dùng mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "Id", "Name", roleId);
                return View();
            }
        }

        // 3. Trang sửa thông tin (GET: /User/Edit/5)
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "Id", "Name", user.RoleId);
            return View(user);
        }

        // Processing Sửa thông tin (POST: /User/Edit/5)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string displayName, string? email, int roleId, bool isActive)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.DisplayName = displayName;
            user.Email = email;
            user.RoleId = roleId;
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật thông tin người dùng thành công!";
            return RedirectToAction(nameof(Index));
        }

        // 4. Khóa / Mở khóa người dùng (POST: /User/ToggleStatus)
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Thông báo dựa trên trạng thái MỚI sau khi lưu
            TempData["Success"] = user.IsActive ? "Đã mở khóa tài khoản thành công!" : "Đã khóa tài khoản thành công!";
            return RedirectToAction(nameof(Index));
        }

        // 5. Đặt lại mật khẩu (POST: /User/ResetPassword)
        [HttpGet]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                ViewBag.Error = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return View(user);
            }

            user.Password = PasswordHasher.HashPassword(newPassword);
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đặt lại mật khẩu thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}