using Microsoft.AspNetCore.Mvc;

namespace NphiesBridge.Web.Controllers
{
    public class IcdMappingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SetupAustralianModification()
        {
            // User selected Australian Modification - no mapping needed
            // Store their choice and redirect to success page
            return View("AustralianModificationSuccess");
        }

        public IActionResult SetupCustomMapping()
        {
            // User selected other system - needs mapping
            return View("MappingSetup");
        }
    }
}