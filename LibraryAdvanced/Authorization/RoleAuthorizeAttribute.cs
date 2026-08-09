using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraryAdvanced.Authorization
{
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string _role;

        public RoleAuthorizeAttribute(string role)
        {
            _role = role;
        }

        public override void OnActionExecuting(
            ActionExecutingContext context)
        {
            var role = context.HttpContext.Session
                .GetString("Role");

            // Chưa đăng nhập
            if (string.IsNullOrEmpty(role))
            {
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Account",
                    null
                );

                return;
            }

            // Không đúng quyền
            if (!role.Equals(
                    _role,
                    StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult(
                    "AccessDenied",
                    "Account",
                    null
                );

                return;
            }

            base.OnActionExecuting(context);
        }
    }
}