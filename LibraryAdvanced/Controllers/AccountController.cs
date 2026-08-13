using LibraryAdvanced.Models;
using LibraryAdvanced.Services;
using LibraryAdvanced.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace LibraryAdvanced.Controllers
{
    public class AccountController : Controller
    {
        private readonly LibraryAdvancedDbContext _context;
        private readonly InterfaceEmailService _emailService;

        public AccountController(LibraryAdvancedDbContext context, InterfaceEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
            if (user == null || !Services.PasswordHasher.VerifyPassword(model.Password, user.Password))
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return base.View(model);
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
                return RedirectToAction("Index", "Home");
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


            user.Password = PasswordHasher.HashPassword(user.Password);
            
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

        // ==========================================
        // QUÊN MẬT KHẨU (GỬI EMAIL)
        // ==========================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Email không hợp lệ.");
                return View();
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);


            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản với email này.");
                return View();
            }



            // Tạo token và lưu vào cơ sở dữ liệu
            string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            user.PasswordResetToken = token;
            user.ResetTokenExpires = DateTime.Now.AddMinutes(30); // Token có hiệu lực trong 30 phút

            await _context.SaveChangesAsync();

            // Gửi email với liên kết đặt lại mật khẩu
            var resetLink = Url.Action("ResetPassword", "Account",
                new { token = token, email = email }, Request.Scheme);

            string emailBody = $@"
            <h3>Yêu cầu đặt lại mật khẩu</h3>
            <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản hệ thống.</p>
            <p>Vui lòng bấm vào liên kết bên dưới để hoàn tất (Liên kết có hiệu lực trong 30 phút):</p>
            <p><a href='{resetLink}' style='padding: 10px 15px; background-color: #0d6efd; color: white; text-decoration: none; border-radius: 5px;'>Đặt lại mật khẩu</a></p>
            <p>Nếu bạn không gửi yêu cầu này, vui lòng bỏ qua email.</p>";

            await _emailService.SendEmailAsync(user.Email, "Đặt lại mật khẩu", emailBody);
            ViewBag.Message = "Một email đặt lại mật khẩu đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư đến.";
            return View();
        }

        // ==========================================
        // TRANG ĐẶT LẠI MẬT KHẨU MỚI
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == email &&
                u.PasswordResetToken == token &&
                u.ResetTokenExpires > DateTime.Now);

            if (user == null)
            {
                ViewBag.Message = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return View("Error");

            }
            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == model.Email &&
                u.PasswordResetToken == model.Token &&
                u.ResetTokenExpires > DateTime.Now);

            if (user == null)
            {
                ModelState.AddModelError("", "Yêu cầu không hợp lệ hoặc đã hết hạn.");
                return View(model);
            }

            // Mã hóa mật khẩu mới bằng PasswordHasher hiện có
            user.Password = PasswordHasher.HashPassword(model.NewPassword);

            // Xóa token sau khi dùng thành công
            user.PasswordResetToken = null;
            user.ResetTokenExpires = null;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
            return RedirectToAction("Login");
        }
    }
}