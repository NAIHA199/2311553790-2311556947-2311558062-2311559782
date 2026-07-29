using LibraryAdvanced.Models;
using LibraryAdvanced.ViewModels;
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
}