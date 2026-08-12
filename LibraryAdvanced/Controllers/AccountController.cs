using LibraryAdvanced.Models;
using LibraryAdvanced.Services;
using LibraryAdvanced.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Tìm user trong Database
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == model.Username.Trim());

            // 2. Kiểm tra sự tồn tại và xác thực mật khẩu băm
            if (user == null || !PasswordHasher.VerifyPassword(model.Password, user.Password))
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // 3. Kiểm tra trạng thái kích hoạt tài khoản
            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin.");
                return View(model);
            }

            // 4. Ghi nhận Đăng nhập (Cookie Auth & Session)
            var roleName = user.Role?.Name ?? "User";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.GivenName, user.Username),
                new Claim(ClaimTypes.Role, roleName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("DisplayName", user.DisplayName);
            HttpContext.Session.SetString("Role", roleName);

            // 5. Điều hướng sau khi đăng nhập thành công
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "User");
            }

            return RedirectToAction("Index", "Home");
        }
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