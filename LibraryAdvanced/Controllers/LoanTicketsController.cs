using LibraryAdvanced.Authorization;
using LibraryAdvanced.Models;
using LibraryAdvanced.ViewModels;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class LoanTicketsController : Controller
{
    private readonly LibraryAdvancedDbContext _context;

    public LoanTicketsController(LibraryAdvancedDbContext context)
    {
        _context = context;
    }

    // YÊU CẦU 2 & 3: Hiển thị danh sách, Tìm kiếm theo tên, Lọc theo trạng thái
    public async Task<IActionResult> Index(string searchString, string statusFilter)
    {
        // =====================================
        // GET CURRENT USER INFO
        // =====================================

        var role = HttpContext.Session.GetString("Role");
        var username = HttpContext.Session.GetString("Username");

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
        // BUILD QUERY
        // =====================================

        var query = _context.LoanTickets
            .Include(lt => lt.LoanDetails)
            .ThenInclude(ld => ld.Book)
            .AsQueryable();

        // Reader: Chỉ xem phiếu của chính mình
        if (role == "Reader")
        {
            query = query.Where(lt => lt.UserId == currentUser!.Id);
        }

        // 1. Tìm kiếm theo Tên người mượn (khớp 1 phần)
        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(lt => lt.BorrowerName.Contains(searchString));
        }

        // 2. Lọc theo Trạng thái (Status)
        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
        {
            query = query.Where(lt => lt.Status == statusFilter);
        }

        var list = await query.Select(lt => new LoanTicketListViewModel
        {
            Id = lt.Id,
            BorrowerName = lt.BorrowerName,
            BorrowDate = lt.BorrowDate,
            Status = lt.Status,
            TotalQuantity = lt.LoanDetails.Sum(ld => ld.Quantity),
            LoanDetails = lt.LoanDetails.Select(ld => new LoanDetailItemViewModel
            {
                BookId = ld.BookId,
                BookTitle = ld.Book!.Title,
                Quantity = ld.Quantity
            }).ToList()
        }).ToListAsync();

        ViewBag.SearchString = searchString;
        ViewBag.StatusFilter = statusFilter;

        return View(list);
    }

    // =====================================
    // SÁCH ĐANG MƯỢN
    // Reader: Chỉ sách của mình
    // Admin: Tất cả sách
    // =====================================

    public async Task<IActionResult> Borrowed()
    {
        var role = HttpContext.Session.GetString("Role");
        var username = HttpContext.Session.GetString("Username");

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

        var query = _context.LoanTickets
            .Include(lt => lt.LoanDetails)
                .ThenInclude(ld => ld.Book)
            .Where(lt => lt.Status == "Borrowed")
            .AsQueryable();

        // Reader: Chỉ xem phiếu của chính mình
        if (role == "Reader")
        {
            query = query.Where(lt => lt.UserId == currentUser!.Id);
        }

        var list = await query
            .OrderByDescending(lt => lt.BorrowDate)
            .ToListAsync();

        return View(list);
    }

    // =====================================
    // LỊCH SỬ MƯỢN
    // Reader: Chỉ lịch sử của mình
    // Admin: Tất cả lịch sử
    // =====================================

    public async Task<IActionResult> History()
    {
        var role = HttpContext.Session.GetString("Role");
        var username = HttpContext.Session.GetString("Username");

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

        var query = _context.LoanTickets
            .Include(lt => lt.LoanDetails)
                .ThenInclude(ld => ld.Book)
            .Where(lt => lt.Status != "Borrowed")
            .AsQueryable();

        // Reader: Chỉ xem phiếu của chính mình
        if (role == "Reader")
        {
            query = query.Where(lt => lt.UserId == currentUser!.Id);
        }

        var list = await query
            .OrderByDescending(lt => lt.BorrowDate)
            .ToListAsync();

        return View(list);
    }
    // GET: LoanTickets/Create

    [RoleAuthorize("Reader")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Books = await _context.Books
            .Where(b => b.AvailableQuantity > 0)
            .OrderBy(b => b.Title)
            .ToListAsync();

        var model = new CreateLoanTicketViewModel();

        // Ban đầu có 1 dòng sách
        model.Details.Add(
            new CreateLoanDetailViewModel
            {
                Quantity = 1
            }
        );

        return View(model);
    }
    // YÊU CẦU 2: Xử lý giao dịch tạo phiếu mượn (Transaction, Rollback, Validation)
    [RoleAuthorize("Reader")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateLoanTicketViewModel model)
    {
        // Kiểm tra có ít nhất 1 sách
        if (model.Details == null ||
            model.Details.Count == 0)
        {
            ModelState.AddModelError(
                "Details",
                "Vui lòng thêm ít nhất một sách."
            );
        }


        // Kiểm tra không được chọn trùng cùng một sách
        if (model.Details != null &&
            model.Details.Count > 0)
        {
            var duplicateBooks = model.Details
                .GroupBy(d => d.BookId)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateBooks.Any())
            {
                ModelState.AddModelError(
                    "Details",
                    "Không được chọn trùng cùng một sách."
                );
            }
        }


        if (!ModelState.IsValid)
        {
            ViewBag.Books = await _context.Books
                .Where(b => b.AvailableQuantity > 0)
                .OrderBy(b => b.Title)
                .ToListAsync();

            return View(model);
        }


        // =========================================
        // TRANSACTION
        // =========================================

        using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // =====================================
            // 1. KIỂM TRA TẤT CẢ SÁCH
            // =====================================

            var bookIds = model.Details
                .Select(d => d.BookId)
                .ToList();

            var books = await _context.Books
                .Where(b => bookIds.Contains(b.Id))
                .ToListAsync();


            // Có sách không tồn tại
            if (books.Count != bookIds.Count)
            {
                ModelState.AddModelError(
                    "Details",
                    "Có sách không tồn tại trong hệ thống."
                );

                await transaction.RollbackAsync();

                ViewBag.Books = await _context.Books
                    .Where(b => b.AvailableQuantity > 0)
                    .OrderBy(b => b.Title)
                    .ToListAsync();

                return View(model);
            }


            // =====================================
            // 2. KIỂM TRA TỒN KHO
            // =====================================

            foreach (var detail in model.Details)
            {
                var book = books.First(
                    b => b.Id == detail.BookId
                );

                if (detail.Quantity <= 0)
                {
                    ModelState.AddModelError(
                        "Details",
                        $"Số lượng mượn của sách \"{book.Title}\" phải lớn hơn 0."
                    );
                }

                if (detail.Quantity > book.AvailableQuantity)
                {
                    ModelState.AddModelError(
                        "Details",
                        $"Sách \"{book.Title}\" không đủ số lượng. " +
                        $"Hiện còn {book.AvailableQuantity} cuốn."
                    );
                }
            }


            // Nếu có lỗi → Rollback
            if (!ModelState.IsValid)
            {
                await transaction.RollbackAsync();

                ViewBag.Books = await _context.Books
                    .Where(b => b.AvailableQuantity > 0)
                    .OrderBy(b => b.Title)
                    .ToListAsync();

                return View(model);
            }


            // =====================================
            // 3. TẠO LOAN TICKET
            // =====================================

            // Get UserId từ session
            var username = HttpContext.Session.GetString("Username");
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                await transaction.RollbackAsync();
                return Unauthorized();
            }

            var loanTicket = new LoanTicket
            {
                BorrowerName = model.BorrowerName,
                BorrowDate = DateTime.Now,
                Status = "Borrowed",
                UserId = user.Id
            };

            _context.LoanTickets.Add(loanTicket);

            await _context.SaveChangesAsync();


            // =====================================
            // 4. TẠO NHIỀU LOAN DETAILS
            // =====================================

            foreach (var detail in model.Details)
            {
                var book = books.First(
                    b => b.Id == detail.BookId
                );


                // Trừ số lượng trong kho
                book.AvailableQuantity -= detail.Quantity;


                // Tạo chi tiết phiếu mượn
                var loanDetail = new LoanDetail
                {
                    LoanTicketId = loanTicket.Id,
                    BookId = detail.BookId,
                    Quantity = detail.Quantity
                };

                _context.LoanDetails.Add(loanDetail);
            }


            // Lưu tất cả thay đổi
            await _context.SaveChangesAsync();


            // =====================================
            // 5. COMMIT
            // =====================================

            await transaction.CommitAsync();


            return RedirectToAction(
                nameof(Index)
            );
        }
        catch (Exception)
        {
            // =====================================
            // ROLLBACK NẾU CÓ LỖI
            // =====================================

            await transaction.RollbackAsync();

            ModelState.AddModelError(
                "",
                "Đã xảy ra lỗi trong quá trình tạo phiếu mượn."
            );
        }


        ViewBag.Books = await _context.Books
            .Where(b => b.AvailableQuantity > 0)
            .OrderBy(b => b.Title)
            .ToListAsync();

        return View(model);
    }
    // =========================================
    // ADMIN - DANH SÁCH SÁCH ĐANG MƯỢN
    // =========================================

    [RoleAuthorize("Admin")]
    [HttpGet]
    public async Task<IActionResult> Return()
    {
        var tickets = await _context.LoanTickets
            .Where(x => x.Status == "Borrowed")
            .Include(x => x.LoanDetails)
                .ThenInclude(ld => ld.Book)
            .OrderByDescending(x => x.BorrowDate)
            .ToListAsync();

        return View("Return", tickets);
    }

    // =========================================
    // ADMIN - XÁC NHẬN TRẢ 1 CUỐN SÁCH
    // =========================================

    [RoleAuthorize("Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmReturn(
        int loanTicketId,
        int bookId)
    {
        using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // =====================================
            // 1. LẤY CHI TIẾT PHIẾU MƯỢN
            // =====================================

            var loanDetail = await _context.LoanDetails
                .Include(ld => ld.Book)
                .Include(ld => ld.LoanTicket)
                .FirstOrDefaultAsync(ld =>
                    ld.LoanTicketId == loanTicketId &&
                    ld.BookId == bookId);

            if (loanDetail == null)
            {
                return NotFound();
            }


            // =====================================
            // 2. KIỂM TRA ĐÃ TRẢ HẾT CHƯA
            // =====================================

            if (loanDetail.ReturnedQuantity >= loanDetail.Quantity)
            {
                TempData["Error"] =
                    "Sách này đã được trả đủ.";

                return RedirectToAction(
                    nameof(Return));
            }


            // =====================================
            // 3. LẤY SÁCH
            // =====================================

            var book = await _context.Books
                .FirstOrDefaultAsync(
                    b => b.Id == bookId);

            if (book == null)
            {
                return NotFound();
            }


            // =====================================
            // 4. TRẢ 1 CUỐN
            // =====================================

            loanDetail.ReturnedQuantity += 1;

            book.AvailableQuantity += 1;


            // =====================================
            // 5. KIỂM TRA PHIẾU ĐÃ TRẢ HẾT CHƯA
            // =====================================

            var loanDetails =
                await _context.LoanDetails
                    .Where(ld =>
                        ld.LoanTicketId == loanTicketId)
                    .ToListAsync();


            bool allReturned =
                loanDetails.All(ld =>
                    ld.ReturnedQuantity >= ld.Quantity);


            if (allReturned)
            {
                loanDetail.LoanTicket.Status =
                    "Returned";
            }
            else
            {
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
    // =========================================
    // IN PHIẾU MƯỢN
    // ADMIN
    // =========================================

    [RoleAuthorize("Admin")]
    [HttpGet]
    public async Task<IActionResult> Print(int id)
    {
        var loanTicket = await _context.LoanTickets
            .Include(lt => lt.LoanDetails)
                .ThenInclude(ld => ld.Book)
            .FirstOrDefaultAsync(lt => lt.Id == id);

        if (loanTicket == null)
        {
            return NotFound();
        }

        return View(loanTicket);
    }
}