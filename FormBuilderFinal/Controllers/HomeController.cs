using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Secret()
        {
            return Content("This is a secret page!");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminSecret()
        {
            return Content("This is an ADMIN secret page!");
        }
    }
}