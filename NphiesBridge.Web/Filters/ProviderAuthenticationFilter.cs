using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NphiesBridge.Web.Services.API;

namespace NphiesBridge.Web.Filters
{
    public class ProviderAuthenticationFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var authService = context.HttpContext.RequestServices.GetService<AuthService>();

            if (authService == null)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Check if user is authenticated
            if (!authService.IsAuthenticated())
            {
                // Check if it's an AJAX request
                if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Result = new JsonResult(new { success = false, message = "Authentication required", redirectUrl = "/Auth/Login" })
                    {
                        StatusCode = 401
                    };
                }
                else
                {
                    var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                    context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
                }
                return;
            }

            // Check if user has Provider or Admin role
            if (!authService.IsInRole("Provider") && !authService.IsInRole("Admin"))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }

    // Convenience attribute
    public class ProviderAuthorizeAttribute : ProviderAuthenticationFilter
    {
        // Inherits all functionality
    }
}