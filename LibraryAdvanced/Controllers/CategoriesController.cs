using LibraryAdvanced.Authorization;
using LibraryAdvanced.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAdvanced.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly LibraryAdvancedDbContext _context;

        public CategoriesController(
            LibraryAdvancedDbContext context)
        {
            _context = context;
        }

        // =========================================
        // DANH SÁCH DANH MỤC
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Categories
                .Include(c => c.Books)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Name.Contains(search));
            }

            ViewBag.Search = search;

            var categories = await query
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }


        // =========================================
        // CREATE - GET
        // =========================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        // =========================================
        // CREATE - POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            // Kiểm tra trùng tên
            var exists = await _context.Categories
                .AnyAsync(c =>
                    c.Name.ToLower() ==
                    category.Name.ToLower());

            if (exists)
            {
                ModelState.AddModelError(
                    "Name",
                    "Danh mục này đã tồn tại.");

                return View(category);
            }

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Thêm danh mục thành công.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================
        // EDIT - GET
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }


        // =========================================
        // EDIT - POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            // Kiểm tra trùng tên với danh mục khác
            var exists = await _context.Categories
                .AnyAsync(c =>
                    c.Id != category.Id &&
                    c.Name.ToLower() ==
                    category.Name.ToLower());

            if (exists)
            {
                ModelState.AddModelError(
                    "Name",
                    "Danh mục này đã tồn tại.");

                return View(category);
            }

            try
            {
                _context.Categories.Update(category);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.Id))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["Success"] =
                "Cập nhật danh mục thành công.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================
        // DELETE - GET
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }


        // =========================================
        // DELETE - POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            // Không cho xóa nếu danh mục đang có sách
            if (category.Books.Any())
            {
                TempData["Error"] =
                    "Không thể xóa danh mục đang có sách. " +
                    "Vui lòng chuyển sách sang danh mục khác trước.";

                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Xóa danh mục thành công.";

            return RedirectToAction(nameof(Index));
        }


        private bool CategoryExists(int id)
        {
            return _context.Categories
                .Any(e => e.Id == id);
        }
    }
}