using LibraryAdvanced.Models;
using LibraryAdvanced.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAdvanced.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly LibraryAdvancedDbContext _context;

        public StatisticsController(LibraryAdvancedDbContext context)
        {
            _context = context;
        }

        // YÊU CẦU 4: Thống kê dữ liệu phức tạp bằng LINQ
        public async Task<IActionResult> Index()
        {
            // 1. Tổng số sách đang được mượn (các phiếu có Status = 'Borrowed')
            var totalActiveBooks = await _context.LoanDetails
                .Where(ld => ld.LoanTicket.Status == "Borrowed")
                .SumAsync(ld => (int?)ld.Quantity) ?? 0;

            // 2. Top 3 cuốn sách được mượn nhiều nhất mọi thời đại (Grouping LINQ)
            var top3Books = await _context.LoanDetails
                .GroupBy(ld => new { ld.BookId, ld.Book.Title })
                .Select(g => new TopBookViewModel
                {
                    BookTitle = g.Key.Title,
                    TotalBorrowedQuantity = g.Sum(ld => ld.Quantity)
                })
                .OrderByDescending(x => x.TotalBorrowedQuantity)
                .Take(3)
                .ToListAsync();

            var viewModel = new StatisticsViewModel
            {
                TotalActiveBorrowedBooks = totalActiveBooks,
                Top3Books = top3Books
            };

            return View(viewModel);
        }
    }
}