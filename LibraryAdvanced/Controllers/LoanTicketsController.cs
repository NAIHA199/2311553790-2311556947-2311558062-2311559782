using LibraryAdvanced.Authorization;
using LibraryAdvanced.Models;
using LibraryAdvanced.ViewModels;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAdvanced.Controllers
{
    public class LoanTicketsController : Controller
    {
        private readonly LibraryAdvancedDbContext _context;

        public LoanTicketsController(
            LibraryAdvancedDbContext context)
        {
            _context = context;
        }


        // =====================================
        // DANH SÁCH PHIẾU MƯỢN
        // =====================================

        public async Task<IActionResult> Index(
            string searchString,
            string statusFilter)
        {
            var role =
                HttpContext.Session.GetString("Role");

            var username =
                HttpContext.Session.GetString("Username");

            User? currentUser = null;

            if (role == "Reader")
            {
                currentUser =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            u => u.Username == username);

                if (currentUser == null)
                {
                    return Unauthorized();
                }
            }


            var query =
                _context.LoanTickets
                    .Include(lt => lt.LoanDetails)
                    .ThenInclude(ld => ld.Book)
                    .AsQueryable();


            // Reader chỉ xem phiếu của mình
            if (role == "Reader")
            {
                query = query.Where(
                    lt => lt.UserId == currentUser!.Id);
            }


            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(
                    lt => lt.BorrowerName
                        .Contains(searchString));
            }


            // Lọc trạng thái
            if (!string.IsNullOrEmpty(statusFilter) &&
                statusFilter != "All")
            {
                query = query.Where(
                    lt => lt.Status == statusFilter);
            }


            var list =
                await query
                    .Select(lt =>
                        new LoanTicketListViewModel
                        {
                            Id = lt.Id,

                            BorrowerName =
                                lt.BorrowerName,

                            BorrowDate =
                                lt.BorrowDate,

                            // ⭐ HẠN TRẢ
                            DueDate =
                                lt.DueDate,

                            // ⭐ NGÀY THỰC TẾ TRẢ
                            ReturnedDate =
                                lt.ReturnedDate,

                            Status =
                                lt.Status,

                            TotalQuantity =
                                lt.LoanDetails
                                    .Sum(ld => ld.Quantity),

                            LoanDetails =
                                lt.LoanDetails
                                    .Select(ld =>
                                        new LoanDetailItemViewModel
                                        {
                                            BookId =
                                                ld.BookId,

                                            BookTitle =
                                                ld.Book!.Title,

                                            Quantity =
                                                ld.Quantity
                                        })
                                    .ToList()
                        })
                    .ToListAsync();


            ViewBag.SearchString =
                searchString;

            ViewBag.StatusFilter =
                statusFilter;


            return View(list);
        }


        // =====================================
        // SÁCH ĐANG MƯỢN
        // =====================================

        public async Task<IActionResult> Borrowed()
        {
            var role =
                HttpContext.Session.GetString("Role");

            var username =
                HttpContext.Session.GetString("Username");

            User? currentUser = null;

            if (role == "Reader")
            {
                currentUser =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            u => u.Username == username);

                if (currentUser == null)
                {
                    return Unauthorized();
                }
            }


            var query =
                _context.LoanTickets
                    .Include(lt => lt.LoanDetails)
                    .ThenInclude(ld => ld.Book)
                    .Where(lt =>
                        lt.Status == "Borrowed")
                    .AsQueryable();


            if (role == "Reader")
            {
                query =
                    query.Where(
                        lt =>
                            lt.UserId ==
                            currentUser!.Id);
            }


            var list =
                await query
                    .OrderByDescending(
                        lt => lt.BorrowDate)
                    .ToListAsync();


            return View(list);
        }


        // =====================================
        // LỊCH SỬ MƯỢN
        // =====================================

        public async Task<IActionResult> History()
        {
            var role =
                HttpContext.Session.GetString("Role");

            var username =
                HttpContext.Session.GetString("Username");

            User? currentUser = null;

            if (role == "Reader")
            {
                currentUser =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            u => u.Username == username);

                if (currentUser == null)
                {
                    return Unauthorized();
                }
            }


            var query =
                _context.LoanTickets
                    .Include(lt => lt.LoanDetails)
                    .ThenInclude(ld => ld.Book)
                    .Where(lt =>
                        lt.Status != "Borrowed")
                    .AsQueryable();


            if (role == "Reader")
            {
                query =
                    query.Where(
                        lt =>
                            lt.UserId ==
                            currentUser!.Id);
            }


            var list =
                await query
                    .OrderByDescending(
                        lt => lt.BorrowDate)
                    .ToListAsync();


            return View(list);
        }


        // =====================================
        // CREATE - GET
        // =====================================

        [RoleAuthorize("Reader")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Books =
                await _context.Books
                    .Where(b =>
                        b.AvailableQuantity > 0)
                    .OrderBy(b => b.Title)
                    .ToListAsync();


            var model =
                new CreateLoanTicketViewModel();


            model.Details.Add(
                new CreateLoanDetailViewModel
                {
                    Quantity = 1
                });


            return View(model);
        }


        // =====================================
        // CREATE - POST
        // =====================================

        [RoleAuthorize("Reader")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateLoanTicketViewModel model)
        {
            // Kiểm tra ít nhất 1 sách
            if (model.Details == null ||
                model.Details.Count == 0)
            {
                ModelState.AddModelError(
                    "Details",
                    "Vui lòng thêm ít nhất một sách.");
            }


            // Kiểm tra sách trùng
            if (model.Details != null &&
                model.Details.Count > 0)
            {
                var duplicateBooks =
                    model.Details
                        .GroupBy(d => d.BookId)
                        .Where(g => g.Count() > 1)
                        .ToList();


                if (duplicateBooks.Any())
                {
                    ModelState.AddModelError(
                        "Details",
                        "Không được chọn trùng cùng một sách.");
                }
            }


            if (!ModelState.IsValid)
            {
                ViewBag.Books =
                    await _context.Books
                        .Where(b =>
                            b.AvailableQuantity > 0)
                        .OrderBy(b => b.Title)
                        .ToListAsync();

                return View(model);
            }


            // =====================================
            // TRANSACTION
            // =====================================

            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();


            try
            {
                // =====================================
                // 1. LẤY SÁCH
                // =====================================

                var bookIds =
                    model.Details
                        .Select(d => d.BookId)
                        .ToList();


                var books =
                    await _context.Books
                        .Where(b =>
                            bookIds.Contains(b.Id))
                        .ToListAsync();


                if (books.Count != bookIds.Count)
                {
                    ModelState.AddModelError(
                        "Details",
                        "Có sách không tồn tại trong hệ thống.");

                    await transaction.RollbackAsync();


                    ViewBag.Books =
                        await _context.Books
                            .Where(b =>
                                b.AvailableQuantity > 0)
                            .OrderBy(b => b.Title)
                            .ToListAsync();


                    return View(model);
                }


                // =====================================
                // 2. KIỂM TRA TỒN KHO
                // =====================================

                foreach (var detail in model.Details)
                {
                    var book =
                        books.First(
                            b => b.Id == detail.BookId);


                    if (detail.Quantity <= 0)
                    {
                        ModelState.AddModelError(
                            "Details",
                            $"Số lượng mượn của sách \"{book.Title}\" phải lớn hơn 0.");
                    }


                    if (detail.Quantity >
                        book.AvailableQuantity)
                    {
                        ModelState.AddModelError(
                            "Details",
                            $"Sách \"{book.Title}\" không đủ số lượng. " +
                            $"Hiện còn {book.AvailableQuantity} cuốn.");
                    }
                }


                if (!ModelState.IsValid)
                {
                    await transaction.RollbackAsync();


                    ViewBag.Books =
                        await _context.Books
                            .Where(b =>
                                b.AvailableQuantity > 0)
                            .OrderBy(b => b.Title)
                            .ToListAsync();


                    return View(model);
                }


                // =====================================
                // 3. LẤY USER
                // =====================================

                var username =
                    HttpContext.Session
                        .GetString("Username");


                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            u =>
                                u.Username ==
                                username);


                if (user == null)
                {
                    await transaction.RollbackAsync();

                    return Unauthorized();
                }


                // =====================================
                // 4. TẠO PHIẾU MƯỢN
                // =====================================

                // Ngày mượn
                var borrowDate =
                    DateTime.Now;


                // Hạn trả = ngày mượn + 7 ngày
                var dueDate =
                    borrowDate.AddDays(7);


                var loanTicket =
                    new LoanTicket
                    {
                        BorrowerName =
                            model.BorrowerName,

                        BorrowDate =
                            borrowDate,

                        DueDate =
                            dueDate,

                        ReturnedDate =
                            null,

                        Status =
                            "Borrowed",

                        UserId =
                            user.Id
                    };


                _context.LoanTickets.Add(
                    loanTicket);


                await _context.SaveChangesAsync();


                // =====================================
                // 5. TẠO NHIỀU CHI TIẾT
                // =====================================

                foreach (var detail in model.Details)
                {
                    var book =
                        books.First(
                            b =>
                                b.Id ==
                                detail.BookId);


                    // Trừ kho
                    book.AvailableQuantity -=
                        detail.Quantity;


                    var loanDetail =
                        new LoanDetail
                        {
                            LoanTicketId =
                                loanTicket.Id,

                            BookId =
                                detail.BookId,

                            Quantity =
                                detail.Quantity
                        };


                    _context.LoanDetails.Add(
                        loanDetail);
                }


                await _context.SaveChangesAsync();


                // =====================================
                // 6. COMMIT
                // =====================================

                await transaction.CommitAsync();


                return RedirectToAction(
                    nameof(Index));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();


                ModelState.AddModelError(
                    "",
                    "Đã xảy ra lỗi trong quá trình tạo phiếu mượn.");
            }


            ViewBag.Books =
                await _context.Books
                    .Where(b =>
                        b.AvailableQuantity > 0)
                    .OrderBy(b => b.Title)
                    .ToListAsync();


            return View(model);
        }


        // =====================================
        // ADMIN - DANH SÁCH SÁCH ĐANG MƯỢN
        // =====================================

        [RoleAuthorize("Admin")]
        [HttpGet]
        public async Task<IActionResult> Return()
        {
            var tickets =
                await _context.LoanTickets
                    .Where(x =>
                        x.Status == "Borrowed")
                    .Include(x =>
                        x.LoanDetails)
                    .ThenInclude(ld =>
                        ld.Book)
                    .OrderByDescending(
                        x => x.BorrowDate)
                    .ToListAsync();


            return View(
                "Return",
                tickets);
        }


        // =====================================
        // ADMIN - XÁC NHẬN TRẢ 1 CUỐN
        // =====================================

        [RoleAuthorize("Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReturn(
            int loanTicketId,
            int bookId)
        {
            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();


            try
            {
                // =====================================
                // 1. LẤY CHI TIẾT
                // =====================================

                var loanDetail =
                    await _context.LoanDetails
                        .Include(ld => ld.Book)
                        .Include(ld => ld.LoanTicket)
                        .FirstOrDefaultAsync(
                            ld =>
                                ld.LoanTicketId ==
                                    loanTicketId
                                &&
                                ld.BookId ==
                                    bookId);


                if (loanDetail == null)
                {
                    return NotFound();
                }


                // =====================================
                // 2. KIỂM TRA ĐÃ TRẢ ĐỦ
                // =====================================

                if (loanDetail.ReturnedQuantity >=
                    loanDetail.Quantity)
                {
                    TempData["Error"] =
                        "Sách này đã được trả đủ.";

                    return RedirectToAction(
                        nameof(Return));
                }


                // =====================================
                // 3. LẤY SÁCH
                // =====================================

                var book =
                    await _context.Books
                        .FirstOrDefaultAsync(
                            b =>
                                b.Id ==
                                bookId);


                if (book == null)
                {
                    return NotFound();
                }


                // =====================================
                // 4. TRẢ 1 CUỐN
                // =====================================

                loanDetail.ReturnedQuantity +=
                    1;

                book.AvailableQuantity +=
                    1;


                // =====================================
                // 5. KIỂM TRA PHIẾU
                // =====================================

                var loanDetails =
                    await _context.LoanDetails
                        .Where(ld =>
                            ld.LoanTicketId ==
                            loanTicketId)
                        .ToListAsync();


                bool allReturned =
                    loanDetails.All(
                        ld =>
                            ld.ReturnedQuantity >=
                            ld.Quantity);


                if (allReturned)
                {
                    // Đã trả hết phiếu
                    loanDetail.LoanTicket.Status =
                        "Returned";

                    // Lưu ngày thực tế trả
                    loanDetail.LoanTicket.ReturnedDate =
                        DateTime.Now;
                }
                else
                {
                    // Vẫn còn sách chưa trả
                    loanDetail.LoanTicket.Status =
                        "Borrowed";
                }


                // =====================================
                // 6. LƯU
                // =====================================

                await _context.SaveChangesAsync();


                // =====================================
                // 7. COMMIT
                // =====================================

                await transaction.CommitAsync();


                TempData["Success"] =
                    $"Đã trả 1 cuốn \"{book.Title}\" thành công.";


                return RedirectToAction(
                    nameof(Return));
            }
            catch
            {
                await transaction.RollbackAsync();


                TempData["Error"] =
                    "Có lỗi xảy ra khi trả sách.";


                return RedirectToAction(
                    nameof(Return));
            }
        }


        // =====================================
        // IN PHIẾU MƯỢN
        // ADMIN
        // =====================================

        [RoleAuthorize("Admin")]
        [HttpGet]
        public async Task<IActionResult> Print(
            int id)
        {
            var loanTicket =
                await _context.LoanTickets
                    .Include(lt =>
                        lt.LoanDetails)
                    .ThenInclude(ld =>
                        ld.Book)
                    .FirstOrDefaultAsync(
                        lt =>
                            lt.Id ==
                            id);


            if (loanTicket == null)
            {
                return NotFound();
            }


            return View(loanTicket);
        }
    }
}