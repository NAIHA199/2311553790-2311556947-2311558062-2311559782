using LibraryAdvanced.Authorization;
using LibraryAdvanced.Models;
using LibraryAdvanced.ViewModel;
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
            int? categoryId,
            string status)
        {
            var query = _context.Books
                .Include(b => b.Category)
                .AsQueryable();

            // Lọc danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(
                    b => b.CategoryId == categoryId.Value);
            }

            // Lọc theo tình trạng (Còn sách / Hết sách)
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "available")
                {
                    query = query.Where(b => b.AvailableQuantity > 0);
                }
                else if (status == "unavailable")
                {
                    query = query.Where(b => b.AvailableQuantity == 0);
                }
            }

            // Chuyển sang client để tìm kiếm
            var books = await query.ToListAsync();

            // Tìm kiếm (case-insensitive + dấu tiếng Việt)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                books = books.Where(b =>
                    b.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    b.Author.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.Categories =
                new SelectList(
                    await _context.Categories.ToListAsync(),
                    "Id",
                    "Name",
                    categoryId
                );

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            books = books
                .OrderBy(b => b.Title)
                .ToList();

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
            CreateBookViewModel model)
        {
            // =====================================
            // KIỂM TRA MODEL
            // =====================================

            if (!ModelState.IsValid)
            {
                await LoadCategories(
                    model.CategoryId);

                return View(model);
            }


            // =====================================
            // KIỂM TRA CATEGORY
            // =====================================

            var category =
                await _context.Categories
                    .FirstOrDefaultAsync(
                        c => c.Id == model.CategoryId);

            if (category == null)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Danh mục không tồn tại.");

                await LoadCategories(
                    model.CategoryId);

                return View(model);
            }


            // =====================================
            // KIỂM TRA ẢNH
            // =====================================

            if (model.ImageFile != null &&
                model.ImageFile.Length > 0)
            {
                var allowedExtensions =
                    new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".webp"
                    };

                var extension =
                    Path.GetExtension(
                        model.ImageFile.FileName)
                        .ToLowerInvariant();

                if (!allowedExtensions.Contains(
                    extension))
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Chỉ cho phép JPG, JPEG, PNG hoặc WEBP.");

                    await LoadCategories(
                        model.CategoryId);

                    return View(model);
                }
            }


            // =====================================
            // TẠO BOOK
            // =====================================

            var book = new Book
            {
                Title = model.Title,
                Author = model.Author,
                CategoryId = model.CategoryId,

                AvailableQuantity = model.AvailableQuantity
            };


            // =====================================
            // LƯU BOOK
            // =====================================

            _context.Books.Add(book);

            await _context.SaveChangesAsync();


            // =====================================
            // UPLOAD ẢNH
            // Sau khi đã có Book.Id
            // =====================================

            if (model.ImageFile != null &&
                model.ImageFile.Length > 0)
            {
                var imagePath = await SaveBookImage(
                    book.Id,
                    model.ImageFile);

                book.ImagePath = imagePath;

                _context.Books.Update(book);

                await _context.SaveChangesAsync();
            }


            // =====================================
            // QUAY VỀ DANH SÁCH
            // =====================================

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
            string Title,
            string Author,
            int CategoryId,
            int AvailableQuantity,
            IFormFile? imageFile)
        {
            // =========================================
            // TÌM SÁCH HIỆN TẠI
            // =========================================

            var existingBook = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id);

            if (existingBook == null)
            {
                return NotFound();
            }


            // =========================================
            // KIỂM TRA DANH MỤC
            // =========================================

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Danh mục không tồn tại.");

                await LoadCategories(CategoryId);

                return View(existingBook);
            }


            // =========================================
            // KIỂM TRA DỮ LIỆU
            // =========================================

            if (string.IsNullOrWhiteSpace(Title))
            {
                ModelState.AddModelError(
                    "Title",
                    "Vui lòng nhập tên sách.");
            }

            if (string.IsNullOrWhiteSpace(Author))
            {
                ModelState.AddModelError(
                    "Author",
                    "Vui lòng nhập tác giả.");
            }

            if (AvailableQuantity < 0)
            {
                ModelState.AddModelError(
                    "AvailableQuantity",
                    "Số lượng không được nhỏ hơn 0.");
            }


            if (!ModelState.IsValid)
            {
                await LoadCategories(CategoryId);

                return View(existingBook);
            }


            // =========================================
            // CẬP NHẬT SÁCH
            // =========================================

            existingBook.Title = Title.Trim();

            existingBook.Author = Author.Trim();

            existingBook.CategoryId = CategoryId;

            existingBook.AvailableQuantity = AvailableQuantity;


            // =========================================
            // LƯU DATABASE
            // =========================================

            await _context.SaveChangesAsync();


            // =========================================
            // THAY ẢNH NẾU CÓ
            // =========================================

            if (imageFile != null &&
                imageFile.Length > 0)
            {
                var imagePath = await SaveBookImage(
                    existingBook.Id,
                    imageFile);

                existingBook.ImagePath = imagePath;

                await _context.SaveChangesAsync();
            }


            // =========================================
            // QUAY VỀ DANH SÁCH
            // =========================================

            return RedirectToAction(nameof(Index));
        }        // =========================================
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

            // Xóa ảnh
            DeleteBookImage(book.Id);

            // Xóa sách
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

        private async Task<string?> SaveBookImage(
            int bookId,
            IFormFile imageFile)
        {
            var folder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "books");


            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }


            // Xóa ảnh cũ
            DeleteBookImage(bookId);


            var extension =
                Path.GetExtension(
                    imageFile.FileName)
                    .ToLowerInvariant();


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


            await imageFile.CopyToAsync(
                stream);

            return $"/uploads/books/{fileName}";
        }


        // =========================================
        // DELETE IMAGE
        // =========================================

        private void DeleteBookImage(
            int bookId)
        {
            var folder =
                Path.Combine(
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


        // =========================================
        // CHECK BOOK
        // =========================================

        private bool BookExists(int id)
        {
            return _context.Books
                .Any(e => e.Id == id);
        }
    }
}