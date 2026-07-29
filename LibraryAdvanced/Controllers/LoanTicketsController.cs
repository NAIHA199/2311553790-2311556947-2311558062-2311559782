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
        var query = _context.LoanTickets
            .Include(lt => lt.LoanDetails)
            .AsQueryable();

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
            TotalQuantity = lt.LoanDetails.Sum(ld => ld.Quantity)
        }).ToListAsync();

        ViewBag.SearchString = searchString;
        ViewBag.StatusFilter = statusFilter;

        return View(list);
    }
    // GET: LoanTickets/Create
    public IActionResult Create()
    {
        ViewBag.Books = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Books, "Id", "Title");
        return View();
    }

    // YÊU CẦU 2: Xử lý giao dịch tạo phiếu mượn (Transaction, Rollback, Validation)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLoanTicketViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Books = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Books, "Id", "Title");
            return View(model);
        }

        // Sử dụng EF Core Transaction
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                var book = await _context.Books.FindAsync(model.BookId);

                if (book == null)
                {
                    ModelState.AddModelError("", "Sách chọn không tồn tại trong hệ thống.");
                    ViewBag.Books = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Books, "Id", "Title");
                    return View(model);
                }

                // KIỂM TRA ĐIỀU KIỆN: Số lượng mượn vượt quá kho -> HỦY GIAO DỊCH
                if (model.Quantity > book.AvailableQuantity)
                {
                    ModelState.AddModelError("", $"Số lượng sách trong kho không đủ! (Hiện còn: {book.AvailableQuantity})");
                    ViewBag.Books = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Books, "Id", "Title");

                    // Rollback không thực hiện bất kỳ thay đổi nào
                    await transaction.RollbackAsync();
                    return View(model);
                }

                // 1. Trừ số lượng sách trong kho
                book.AvailableQuantity -= model.Quantity;

                // 2. Tạo bản ghi LoanTickets mới (Mặc định status 'Borrowed')
                var loanTicket = new LoanTicket
                {
                    BorrowerName = model.BorrowerName,
                    BorrowDate = DateTime.Now,
                    Status = "Borrowed"
                };
                _context.LoanTickets.Add(loanTicket);
                await _context.SaveChangesAsync(); // Lấy được LoanTicket.Id vừa sinh

                // 3. Tạo bản ghi LoanDetails tương ứng
                var loanDetail = new LoanDetail
                {
                    LoanTicketId = loanTicket.Id,
                    BookId = model.BookId,
                    Quantity = model.Quantity
                };
                _context.LoanDetails.Add(loanDetail);
                await _context.SaveChangesAsync();

                // COMMIT Transaction nếu tất cả thành công
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                // Tự động Rollback nếu xảy ra lỗi ngoại lệ
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống trong quá trình thực hiện giao dịch.");
            }
        }

        ViewBag.Books = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Books, "Id", "Title");
        return View(model);
    }
}