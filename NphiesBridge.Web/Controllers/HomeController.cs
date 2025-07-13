using Microsoft.AspNetCore.Mvc;
using NphiesBridge.Web.Filters;

namespace NphiesBridge.Web.Controllers
{
    [ProviderAuthorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}