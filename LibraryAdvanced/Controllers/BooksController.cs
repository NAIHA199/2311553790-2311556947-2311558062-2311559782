using LibraryAdvanced.Authorization;
using LibraryAdvanced.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryAdvanced.Controllers
{
    public class BooksController : Controller
    {
        private readonly LibraryAdvancedDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public BooksController(
            LibraryAdvancedDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // =========================================
        // DANH SÁCH SÁCH
        // Reader + Admin
        // =========================================

        public async Task<IActionResult> Index(
            string searchString,
            int? categoryId)
        {
            var query = _context.Books
                .Include(b => b.Category)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(b =>
                    b.Title.Contains(searchString) ||
                    b.Author.Contains(searchString));
            }

            // Lọc danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(
                    b => b.CategoryId == categoryId.Value);
            }

            ViewBag.Categories =
                new SelectList(
                    await _context.Categories.ToListAsync(),
                    "Id",
                    "Name",
                    categoryId
                );

            ViewBag.SearchString = searchString;

            var books = await query
                .OrderBy(b => b.Title)
                .ToListAsync();

            return View(books);
        }


        // =========================================
        // CHI TIẾT
        // Reader + Admin
        // =========================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Category)
                .FirstOrDefaultAsync(
                    b => b.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }


        // =========================================
        // CREATE - GET
        // ADMIN
        // =========================================

        [RoleAuthorize("Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategories();

            return View();
        }


        // =========================================
        // CREATE - POST
        // ADMIN
        // =========================================

        [RoleAuthorize("Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Book book,
            IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategories(
                    book.CategoryId);

                return View(book);
            }

            _context.Books.Add(book);

            await _context.SaveChangesAsync();

            // Upload ảnh sau khi có Book.Id
            if (imageFile != null &&
                imageFile.Length > 0)
            {
                await SaveBookImage(
                    book.Id,
                    imageFile);
            }

            return RedirectToAction(
                nameof(Index));
        }


        // =========================================
        // EDIT - GET
        // ADMIN
        // =========================================

        [RoleAuthorize("Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .FindAsync(id);

            if (book == null)
            {
                return NotFound();
            }

            await LoadCategories(
                book.CategoryId);

            return View(book);
        }


        // =========================================
        // EDIT - POST
        // ADMIN
        // =========================================

        [RoleAuthorize("Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Book book,
            IFormFile? imageFile)
        {
            if (id != book.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadCategories(
                    book.CategoryId);

                return View(book);
            }

            try
            {
                _context.Update(book);

                await _context.SaveChangesAsync();

                if (imageFile != null &&
                    imageFile.Length > 0)
                {
                    await SaveBookImage(
                        book.Id,
                        imageFile);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(book.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(
                nameof(Index));
        }


        // =========================================
        // DELETE - GET
        // ADMIN
        // =========================================

        [RoleAuthorize("Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Category)
                .FirstOrDefaultAsync(
                    b => b.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }


        // =========================================
        // DELETE - POST
        // ADMIN
        // =========================================

        [RoleAuthorize("Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var book = await _context.Books
                .FindAsync(id);

            if (book == null)
            {
                return NotFound();
            }

            // Xóa ảnh của sách
            DeleteBookImage(book.Id);

            _context.Books.Remove(book);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index));
        }


        // =========================================
        // CATEGORY
        // =========================================

        private async Task LoadCategories(
            int? selectedId = null)
        {
            ViewBag.Categories =
                new SelectList(
                    await _context.Categories
                        .OrderBy(c => c.Name)
                        .ToListAsync(),
                    "Id",
                    "Name",
                    selectedId
                );
        }


        // =========================================
        // SAVE IMAGE
        // =========================================

        private async Task SaveBookImage(
            int bookId,
            IFormFile imageFile)
        {
            var folder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "books");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // Xóa ảnh cũ trước
            DeleteBookImage(bookId);

            var extension =
                Path.GetExtension(
                    imageFile.FileName);

            var fileName =
                $"{bookId}{extension}";

            var filePath =
                Path.Combine(
                    folder,
                    fileName);

            using var stream =
                new FileStream(
                    filePath,
                    FileMode.Create);

            await imageFile.CopyToAsync(stream);
        }


        // =========================================
        // DELETE IMAGE
        // =========================================

        private void DeleteBookImage(
            int bookId)
        {
            var folder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "books");

            if (!Directory.Exists(folder))
            {
                return;
            }

            var files =
                Directory.GetFiles(
                    folder,
                    $"{bookId}.*");

            foreach (var file in files)
            {
                System.IO.File.Delete(file);
            }
        }


        private bool BookExists(int id)
        {
            return _context.Books
                .Any(e => e.Id == id);
        }
    }
}