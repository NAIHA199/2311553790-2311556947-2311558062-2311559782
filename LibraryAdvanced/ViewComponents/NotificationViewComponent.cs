using LibraryAdvanced.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAdvanced.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly LibraryAdvancedDbContext _context;

        public NotificationViewComponent(
            LibraryAdvancedDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy username của người đang đăng nhập
            var username =
                HttpContext.Session.GetString("Username");

            // Chưa đăng nhập
            if (string.IsNullOrEmpty(username))
            {
                return View(0);
            }

            // Tìm User
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Username == username);

            // Không tìm thấy User
            if (user == null)
            {
                return View(0);
            }

            // Ngày hiện tại
            var today =
                DateTime.Now.Date;

            // Ngày mai
            var tomorrow =
                today.AddDays(1);

            // Đếm số phiếu cần thông báo
            var count =
                await _context.LoanTickets
                    .Where(lt =>
                        lt.UserId == user.Id
                        &&
                        lt.Status == "Borrowed"
                        &&
                        lt.DueDate.HasValue
                        &&
                        lt.DueDate.Value.Date <= tomorrow)
                    .CountAsync();

            return View(count);
        }
    }
}