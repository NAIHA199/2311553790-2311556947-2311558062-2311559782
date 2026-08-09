using LibraryAdvanced.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace LibraryAdvanced.Controllers
{
    public class HomeController : Controller
    {
        private readonly LibraryAdvancedDbContext _context;

        public HomeController(LibraryAdvancedDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // =====================================
            // GET CURRENT USER INFO
            // =====================================

            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");
            var displayName = HttpContext.Session.GetString("DisplayName");

            User? currentUser = null;
            if (role == "Reader")
            {
                currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (currentUser == null)
                {
                    return Unauthorized();
                }
            }


            // =====================================
            // 1. TỔNG SỐ SÁCH
            // (Cả Admin và Reader đều xem tổng)
            // =====================================

            var totalBooks = await _context.Books.CountAsync();


            // =====================================
            // 2. SÁCH ĐANG MƯỢN
            // =====================================

            int borrowedBooks;

            if (role == "Reader")
            {
                // Reader: chỉ sách của chính mình
                borrowedBooks = await _context.LoanDetails
                    .Where(ld => ld.LoanTicket.Status == "Borrowed" && 
                                 ld.LoanTicket.UserId == currentUser!.Id)
                    .SumAsync(ld => (int?)ld.Quantity) ?? 0;
            }
            else
            {
                // Admin: tất cả sách đang mượn
                borrowedBooks = await _context.LoanDetails
                    .Where(ld => ld.LoanTicket.Status == "Borrowed")
                    .SumAsync(ld => (int?)ld.Quantity) ?? 0;
            }


            // =====================================
            // 3. LỊCH SỬ MƯỢN (TỔNG PHIẾU)
            // =====================================

            int totalLoans;

            if (role == "Reader")
            {
                // Reader: chỉ phiếu của chính mình
                totalLoans = await _context.LoanTickets
                    .Where(lt => lt.UserId == currentUser!.Id)
                    .CountAsync();
            }
            else
            {
                // Admin: tất cả phiếu
                totalLoans = await _context.LoanTickets.CountAsync();
            }


            // =====================================
            // 4. SÁCH NỔI BẬT (4 SÁCH NGẪU NHIÊN)
            // =====================================

            var featuredBooks = await _context.Books
                .Include(b => b.Category)
                .OrderBy(b => EF.Functions.Random())
                .Take(4)
                .ToListAsync();


            // =====================================
            // 5. DANH MỤC
            // =====================================

            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();


            // =====================================
            // TRUYỀN DỮ LIỆU
            // =====================================

            ViewBag.TotalBooks = totalBooks;
            ViewBag.BorrowedBooks = borrowedBooks;
            ViewBag.TotalLoans = totalLoans;
            ViewBag.FeaturedBooks = featuredBooks;
            ViewBag.Categories = categories;
            ViewBag.UserRole = role;
            ViewBag.DisplayName = displayName;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
