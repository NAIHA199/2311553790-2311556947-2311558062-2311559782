using LibraryAdvanced.Models;
using LibraryAdvanced.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAdvanced.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly LibraryAdvancedDbContext _context;

        public NotificationsController(
            LibraryAdvancedDbContext context)
        {
            _context = context;
        }


        // =========================================
        // DANH SÁCH THÔNG BÁO
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Lấy username đang đăng nhập
            var username =
                HttpContext.Session.GetString("Username");


            // Nếu chưa đăng nhập
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // Lấy User hiện tại
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.Username ==
                            username);


            if (user == null)
            {
                return Unauthorized();
            }


            // =====================================
            // LẤY PHIẾU ĐANG MƯỢN
            // CỦA USER HIỆN TẠI
            // =====================================

            var tickets =
                await _context.LoanTickets

                    .Include(lt =>
                        lt.LoanDetails)

                    .Where(lt =>
                        lt.UserId == user.Id
                        &&
                        lt.Status == "Borrowed"
                        &&
                        lt.DueDate.HasValue)

                    .OrderBy(lt =>
                        lt.DueDate)

                    .ToListAsync();


            var today =
                DateTime.Now.Date;


            var notifications =
                new List<NotificationViewModel>();


            foreach (var ticket in tickets)
            {
                var dueDate =
                    ticket.DueDate!.Value.Date;


                // =================================
                // QUÁ HẠN
                // =================================

                if (today > dueDate)
                {
                    var overdueDays =
                        (today - dueDate).Days;


                    notifications.Add(
                        new NotificationViewModel
                        {
                            LoanTicketId =
                                ticket.Id,

                            Type =
                                "Overdue",

                            Message =
                                $"Phiếu mượn #{ticket.Id} " +
                                $"đã quá hạn {overdueDays} ngày.",

                            DueDate =
                                ticket.DueDate,

                            Days =
                                overdueDays,

                            TotalQuantity =
                                ticket.LoanDetails
                                    .Sum(ld =>
                                        ld.Quantity)
                        });

                    continue;
                }


                // =================================
                // SẮP ĐẾN HẠN
                // =================================

                var remainingDays =
                    (dueDate - today).Days;


                if (remainingDays <= 1)
                {
                    notifications.Add(
                        new NotificationViewModel
                        {
                            LoanTicketId =
                                ticket.Id,

                            Type =
                                "DueSoon",

                            Message =
                                remainingDays == 0
                                    ? $"Phiếu mượn #{ticket.Id} " +
                                      "đến hạn trả hôm nay."
                                    : $"Phiếu mượn #{ticket.Id} " +
                                      "sẽ đến hạn trả ngày mai.",

                            DueDate =
                                ticket.DueDate,

                            Days =
                                remainingDays,

                            TotalQuantity =
                                ticket.LoanDetails
                                    .Sum(ld =>
                                        ld.Quantity)
                        });
                }
            }


            return View(notifications);
        }
    }
}