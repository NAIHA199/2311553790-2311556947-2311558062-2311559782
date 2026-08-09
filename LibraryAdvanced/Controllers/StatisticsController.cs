using LibraryAdvanced.Models;
using LibraryAdvanced.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAdvanced.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly LibraryAdvancedDbContext _context;

        public StatisticsController(
            LibraryAdvancedDbContext context)
        {
            _context = context;
        }


        // =========================================
        // DASHBOARD
        // =========================================

        public async Task<IActionResult> Index(
            DateTime? TuNgay,
            DateTime? DenNgay)
        {
            // =====================================
            // 1. GIỮ NGUYÊN:
            // TỔNG SỐ SÁCH ĐANG ĐƯỢC MƯỢN
            // =====================================

            var totalActiveBooks =
                await _context.LoanDetails

                    .Where(ld =>
                        ld.LoanTicket.Status == "Borrowed")

                    .SumAsync(ld =>
                        (int?)ld.Quantity)

                    ?? 0;


            // =====================================
            // 2. GIỮ NGUYÊN:
            // TOP 3 SÁCH MƯỢN NHIỀU NHẤT
            // =====================================

            var top3Books =
                await _context.LoanDetails

                    .GroupBy(ld => new
                    {
                        ld.BookId,
                        ld.Book.Title
                    })

                    .Select(g =>
                        new TopBookViewModel
                        {
                            BookTitle = g.Key.Title,

                            TotalBorrowedQuantity =
                                g.Sum(ld => ld.Quantity)
                        })

                    .OrderByDescending(x =>
                        x.TotalBorrowedQuantity)

                    .Take(3)

                    .ToListAsync();


            // =====================================
            // 3. MẶC ĐỊNH 30 NGÀY
            // CHO BIỂU ĐỒ
            // =====================================

            const int KHOANG_CACH_NGAY = 30;


            if (!TuNgay.HasValue &&
                !DenNgay.HasValue)
            {
                DenNgay = DateTime.Now.Date;

                TuNgay =
                    DenNgay.Value.AddDays(
                        -KHOANG_CACH_NGAY);
            }

            else if (
                TuNgay.HasValue &&
                !DenNgay.HasValue)
            {
                DenNgay =
                    TuNgay.Value.AddDays(
                        KHOANG_CACH_NGAY);
            }

            else if (
                !TuNgay.HasValue &&
                DenNgay.HasValue)
            {
                TuNgay =
                    DenNgay.Value.AddDays(
                        -KHOANG_CACH_NGAY);
            }


            // =====================================
            // 4. BIỂU ĐỒ
            // SÁCH ĐƯỢC MƯỢN THEO NGÀY
            // =====================================

            var denNgayExclusive =
                DenNgay!.Value.Date.AddDays(1);


            var chartData =
                await _context.LoanDetails

                    .Where(ld =>
                        ld.LoanTicket.BorrowDate >=
                            TuNgay!.Value.Date

                        &&

                        ld.LoanTicket.BorrowDate <
                            denNgayExclusive
                    )

                    .GroupBy(ld => new
                    {
                        ld.BookId,
                        ld.Book.Title
                    })

                    .Select(g => new
                    {
                        BookTitle = g.Key.Title,

                        SoLuongMuon =
                            g.Sum(ld => ld.Quantity)
                    })

                    .OrderByDescending(x =>
                        x.SoLuongMuon)

                    .ToListAsync();


            // =====================================
            // 5. VIEW MODEL CŨ
            // =====================================

            var viewModel =
                new StatisticsViewModel
                {
                    TotalActiveBorrowedBooks =
                        totalActiveBooks,

                    Top3Books =
                        top3Books
                };


            // =====================================
            // 6. DỮ LIỆU CHO CHART
            // =====================================

            ViewBag.ChartData =
                chartData;

            ViewBag.TuNgay =
                TuNgay.Value.ToString("yyyy-MM-dd");

            ViewBag.DenNgay =
                DenNgay.Value.ToString("yyyy-MM-dd");


            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> GetBorrowedBooksChart(
        DateTime? TuNgay,
        DateTime? DenNgay)
        {
            const int KHOANG_CACH_NGAY = 30;


            if (!TuNgay.HasValue &&
                !DenNgay.HasValue)
            {
                DenNgay = DateTime.Now.Date;

                TuNgay =
                    DenNgay.Value.AddDays(
                        -KHOANG_CACH_NGAY);
            }

            else if (
                TuNgay.HasValue &&
                !DenNgay.HasValue)
            {
                DenNgay =
                    TuNgay.Value.AddDays(
                        KHOANG_CACH_NGAY);
            }

            else if (
                !TuNgay.HasValue &&
                DenNgay.HasValue)
            {
                TuNgay =
                    DenNgay.Value.AddDays(
                        -KHOANG_CACH_NGAY);
            }


            var denNgayExclusive =
                DenNgay!.Value.Date.AddDays(1);


            var data =
                await _context.LoanDetails

                    .Where(ld =>
                        ld.LoanTicket.BorrowDate >=
                            TuNgay!.Value.Date

                        &&

                        ld.LoanTicket.BorrowDate <
                            denNgayExclusive
                    )

                    .GroupBy(ld => new
                    {
                        ld.BookId,
                        ld.Book.Title
                    })

                    .Select(g => new
                    {
                        BookTitle =
                            g.Key.Title,

                        SoLuongMuon =
                            g.Sum(ld => ld.Quantity)
                    })

                    .OrderByDescending(x =>
                        x.SoLuongMuon)

                    .ToListAsync();


            return Json(data);
        }

    }

}