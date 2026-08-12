using ClosedXML.Excel;
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

            if (!TuNgay.HasValue && !DenNgay.HasValue)
            {
                DenNgay = DateTime.Now.Date;
                TuNgay = DenNgay.Value.AddDays(-KHOANG_CACH_NGAY);
            }
            else if (TuNgay.HasValue && !DenNgay.HasValue)
            {
                DenNgay = TuNgay.Value.AddDays(KHOANG_CACH_NGAY);
            }
            else if (!TuNgay.HasValue && DenNgay.HasValue)
            {
                TuNgay = DenNgay.Value.AddDays(-KHOANG_CACH_NGAY);
            }

            if (TuNgay > DenNgay)
            {
                return BadRequest("Từ ngày không được lớn hơn đến ngày.");
            }

            var denNgayExclusive =
                DenNgay!.Value.Date.AddDays(1);

            var data = await _context.LoanDetails
                .Where(ld =>
                    ld.LoanTicket.BorrowDate >= TuNgay!.Value.Date &&
                    ld.LoanTicket.BorrowDate < denNgayExclusive
                )
                .GroupBy(ld => new
                {
                    BookId = ld.BookId,
                    BookTitle = ld.Book.Title
                })
                .Select(g => new
                {
                    BookTitle = g.Key.BookTitle,
                    SoLuongMuon = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.SoLuongMuon)
                .ToListAsync();

            return Json(data);
        }



        // =========================================
        // XUẤT BÁO CÁO THỐNG KÊ EXCEL
        // =========================================

        [HttpGet]
        public async Task<IActionResult> ExportExcel(
        DateTime? TuNgay,
        DateTime? DenNgay)
        {
            const int KHOANG_CACH_NGAY = 30;

            // ================================
            // XỬ LÝ KHOẢNG NGÀY
            // ================================

            if (!TuNgay.HasValue && !DenNgay.HasValue)
            {
                DenNgay = DateTime.Now.Date;

                TuNgay = DenNgay.Value.AddDays(
                    -KHOANG_CACH_NGAY);
            }
            else if (TuNgay.HasValue && !DenNgay.HasValue)
            {
                DenNgay = TuNgay.Value.AddDays(
                    KHOANG_CACH_NGAY);
            }
            else if (!TuNgay.HasValue && DenNgay.HasValue)
            {
                TuNgay = DenNgay.Value.AddDays(
                    -KHOANG_CACH_NGAY);
            }

            var tuNgayDate = TuNgay!.Value.Date;

            var denNgayExclusive =
                DenNgay!.Value.Date.AddDays(1);


            // ================================
            // 1. TỔNG SÁCH ĐANG MƯỢN
            // ================================

            var totalActiveBooks =
                await _context.LoanDetails
                    .Where(ld =>
                        ld.LoanTicket.Status == "Borrowed")
                    .SumAsync(ld =>
                        (int?)ld.Quantity) ?? 0;


            // ================================
            // 2. TOP 3 SÁCH
            // ================================

            var top3Books =
                await _context.LoanDetails

                    .GroupBy(ld => new
                    {
                        ld.BookId,
                        ld.Book.Title
                    })

                    .Select(g => new
                    {
                        BookTitle = g.Key.Title,

                        TotalBorrowedQuantity =
                            g.Sum(ld => ld.Quantity)
                    })

                    .OrderByDescending(x =>
                        x.TotalBorrowedQuantity)

                    .Take(3)

                    .ToListAsync();


            // ================================
            // 3. THỐNG KÊ THEO KHOẢNG NGÀY
            // ================================

            var chartData =
                await _context.LoanDetails

                    .Where(ld =>
                        ld.LoanTicket.BorrowDate >=
                            tuNgayDate

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


            // ================================
            // TẠO FILE EXCEL
            // ================================

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add(
                    "BaoCaoThongKe");


            // ================================
            // TIÊU ĐỀ
            // ================================

            worksheet.Cell("A1")
                .Value = "LIBRARY MANAGEMENT";

            worksheet.Cell("A2")
                .Value = "BÁO CÁO THỐNG KÊ MƯỢN SÁCH";


            worksheet.Range("A1:D1")
                .Merge();

            worksheet.Range("A2:D2")
                .Merge();


            worksheet.Cell("A1")
                .Style.Font.Bold = true;

            worksheet.Cell("A1")
                .Style.Font.FontSize = 18;

            worksheet.Cell("A1")
                .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;


            worksheet.Cell("A2")
                .Style.Font.Bold = true;

            worksheet.Cell("A2")
                .Style.Font.FontSize = 14;

            worksheet.Cell("A2")
                .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;


            // ================================
            // THỜI GIAN
            // ================================

            worksheet.Cell("A4")
                .Value = "Từ ngày";

            worksheet.Cell("B4")
                .Value = tuNgayDate;

            worksheet.Cell("C4")
                .Value = "Đến ngày";

            worksheet.Cell("D4")
                .Value = DenNgay.Value.Date;


            worksheet.Cell("B4")
                .Style.DateFormat.Format =
                    "dd/MM/yyyy";

            worksheet.Cell("D4")
                .Style.DateFormat.Format =
                    "dd/MM/yyyy";


            // ================================
            // TỔNG QUAN
            // ================================

            worksheet.Cell("A6")
                .Value = "TỔNG QUAN";


            worksheet.Cell("A7")
                .Value = "Sách đang được mượn";

            worksheet.Cell("B7")
                .Value = totalActiveBooks;


            // ================================
            // TOP 3
            // ================================

            worksheet.Cell("A9")
                .Value =
                    "TOP 3 SÁCH ĐƯỢC MƯỢN NHIỀU NHẤT";


            worksheet.Cell("A10")
                .Value = "STT";

            worksheet.Cell("B10")
                .Value = "Tên sách";

            worksheet.Cell("C10")
                .Value = "Số lượng";


            int row = 11;
            int stt = 1;


            foreach (var book in top3Books)
            {
                worksheet.Cell(row, 1)
                    .Value = stt++;

                worksheet.Cell(row, 2)
                    .Value = book.BookTitle;

                worksheet.Cell(row, 3)
                    .Value =
                        book.TotalBorrowedQuantity;

                row++;
            }


            // ================================
            // CHI TIẾT THỐNG KÊ
            // ================================

            row += 2;

            worksheet.Cell(row, 1)
                .Value =
                    "THỐNG KÊ SÁCH ĐƯỢC MƯỢN";


            row++;

            worksheet.Cell(row, 1)
                .Value = "STT";

            worksheet.Cell(row, 2)
                .Value = "Tên sách";

            worksheet.Cell(row, 3)
                .Value = "Số lượng đã mượn";


            row++;

            stt = 1;


            foreach (var item in chartData)
            {
                worksheet.Cell(row, 1)
                    .Value = stt++;

                worksheet.Cell(row, 2)
                    .Value = item.BookTitle;

                worksheet.Cell(row, 3)
                    .Value = item.SoLuongMuon;

                row++;
            }


            // ================================
            // FORMAT BẢNG
            // ================================

            var usedRange =
                worksheet.RangeUsed();

            if (usedRange != null)
            {
                usedRange.Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

                usedRange.Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

                usedRange.Style.Border.InsideBorder =
                    XLBorderStyleValues.Thin;
            }


            // Header các bảng
            worksheet.Range("A10:C10")
                .Style.Font.Bold = true;

            worksheet.Range("A10:C10")
                .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;


            // ================================
            // AUTO WIDTH
            // ================================

            worksheet.Columns()
                .AdjustToContents();


            // Giới hạn độ rộng tên sách
            worksheet.Column(2)
                .Width = 40;


            // ================================
            // XUẤT FILE
            // ================================

            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;


            var fileName =
                $"BaoCaoThongKe_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";


            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

    }
}