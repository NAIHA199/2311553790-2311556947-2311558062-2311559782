using LibraryAdvanced.Models;
using LibraryAdvanced.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAdvanced.Controllers
{
    public class AccountController : Controller
    {
        private readonly LibraryAdvancedDbContext _context;

        public AccountController(LibraryAdvancedDbContext context)
        {
            _context = context;
        }

        // =========================================
        // LOGIN - GET
        // =========================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // =========================================
        // LOGIN - POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // =====================================
            // ADMIN DEMO
            // =====================================

            if (model.Username == "admin" &&
                model.Password == "123456")
            {
                HttpContext.Session.SetString(
                    "Username",
                    "admin"
                );

                HttpContext.Session.SetString(
                    "DisplayName",
                    "Admin"
                );

                HttpContext.Session.SetString(
                    "Role",
                    "Admin"
                );

                return RedirectToAction(
                    "Index",
                    "Home"
                );
            }


            // =====================================
            // LOGIN USER TRONG DATABASE
            // =====================================

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Username == model.Username &&
                    u.Password == model.Password &&
                    u.IsActive == true
                );


            if (user != null)
            {
                HttpContext.Session.SetString(
                    "Username",
                    user.Username
                );

                HttpContext.Session.SetString(
                    "DisplayName",
                    user.DisplayName
                );

                // Role lấy từ DB
                HttpContext.Session.SetString(
                    "Role",
                    user.Role?.Name ?? "Reader"
                );

                return RedirectToAction(
                    "Index",
                    "Home"
                );
            }


            // =====================================
            // LOGIN SAI
            // =====================================

            ModelState.AddModelError(
                "",
                "Tên đăng nhập hoặc mật khẩu không đúng."
            );

            return View(model);
        }


        // =========================================
        // REGISTER - GET
        // =========================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        // =========================================
        // REGISTER - POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // =====================================
            // KIỂM TRA USERNAME ĐÃ TỒN TẠI
            // =====================================

            var usernameExists =
                await _context.Users
                    .AnyAsync(u =>
                        u.Username == model.Username);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập đã tồn tại."
                );

                return View(model);
            }


            // =====================================
            // KIỂM TRA EMAIL
            // =====================================

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var emailExists =
                    await _context.Users
                        .AnyAsync(u =>
                            u.Email == model.Email);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Email",
                        "Email này đã được sử dụng."
                    );

                    return View(model);
                }
            }


            // =====================================
            // LẤY ROLE READER
            // =====================================
            //
            // DB của project hiện tại:
            // Admin = 1
            // Reader = 2
            //
            // QUAN TRỌNG:
            // Người dùng KHÔNG được truyền RoleId
            // từ form đăng ký.
            // =====================================

            const int readerRoleId = 2;


            // =====================================
            // TẠO USER
            // =====================================

            var user = new User
            {
                Username = model.Username.Trim(),

                Password = model.Password,

                DisplayName = model.DisplayName.Trim(),

                Email = string.IsNullOrWhiteSpace(model.Email)
                    ? null
                    : model.Email.Trim(),

                // LUÔN LUÔN LÀ READER
                RoleId = readerRoleId,

                IsActive = true,

                CreatedAt = DateTime.Now,

                UpdatedAt = null
            };


            _context.Users.Add(user);

            await _context.SaveChangesAsync();


            // =====================================
            // ĐĂNG KÝ THÀNH CÔNG
            // =====================================

            TempData["SuccessMessage"] =
                "Đăng ký tài khoản thành công! " +
                "Vui lòng đăng nhập.";

            return RedirectToAction("Login");
        }


        // =========================================
        // LOGOUT
        // =========================================

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "Account"
            );
        }


        // =========================================
        // PROFILE
        // =========================================

        public IActionResult Profile()
        {
            var username =
                HttpContext.Session.GetString("Username");

            var displayName =
                HttpContext.Session.GetString("DisplayName");

            var role =
                HttpContext.Session.GetString("Role");


            // Chưa đăng nhập
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }


            ViewBag.Username = username;

            ViewBag.DisplayName =
                displayName ?? "Người dùng";

            ViewBag.Role =
                role ?? "Reader";


            return View();
        }
    }
}