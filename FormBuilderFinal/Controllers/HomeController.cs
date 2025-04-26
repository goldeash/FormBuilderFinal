using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormBuilder.Data;
using FormBuilder.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var templates = new List<Template>();

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                if (User.IsInRole("Admin"))
                {
                    templates = await _context.Templates
                        .Include(t => t.User)
                        .OrderByDescending(t => t.CreatedDate)
                        .ToListAsync();
                }
                else
                {
                    templates = await _context.Templates
                        .Include(t => t.User)
                        .Where(t => t.IsPublic ||
                                    t.AllowedUsers.Any(au => au.UserId == user.Id) ||
                                    t.UserId == user.Id)
                        .OrderByDescending(t => t.CreatedDate)
                        .ToListAsync();
                }
            }
            else
            {
                templates = await _context.Templates
                    .Include(t => t.User)
                    .Where(t => t.IsPublic)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToListAsync();
            }

            return View(templates);
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