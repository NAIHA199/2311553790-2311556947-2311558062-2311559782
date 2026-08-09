using LibraryAdvanced.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAdvanced.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ADMIN
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


            // READER
            if (model.Username == "reader" &&
                model.Password == "123456")
            {
                HttpContext.Session.SetString(
                    "Username",
                    "reader"
                );

                HttpContext.Session.SetString(
                    "DisplayName",
                    "Nguyễn Văn A"
                );

                HttpContext.Session.SetString(
                    "Role",
                    "Reader"
                );

                return RedirectToAction(
                    "Index",
                    "Home"
                );
            }


            // ĐĂNG NHẬP SAI

            ModelState.AddModelError(
                "",
                "Tên đăng nhập hoặc mật khẩu không đúng."
            );

            return View(model);
        }
        // Đăng xuất 

        [HttpGet]
        public IActionResult Logout()
        {
            return RedirectToAction("Login", "Account");
        }
    
        // =========================================
        // HỒ SƠ
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