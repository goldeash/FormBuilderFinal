using FormBuilder.Data;
using FormBuilder.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly bool _fullTextSearchEnabled;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            _fullTextSearchEnabled = CheckFullTextSearchEnabled();
        }

        private bool CheckFullTextSearchEnabled()
        {
            try
            {
                return _context.Database.ExecuteSqlRaw(
                    "SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Templates')") > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IActionResult> Index()
        {
            var templates = await GetTemplatesForCurrentUser();
            return View(templates);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index");
            }

            ViewBag.SearchQuery = query;
            var templates = await SearchTemplates(query);
            return View("Index", templates);
        }

        private async Task<List<Template>> GetTemplatesForCurrentUser()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                if (User.IsInRole("Admin"))
                {
                    return await _context.Templates
                        .Include(t => t.User)
                        .Include(t => t.Tags)
                        .OrderByDescending(t => t.CreatedDate)
                        .ToListAsync();
                }

                return await _context.Templates
                    .Include(t => t.User)
                    .Include(t => t.Tags)
                    .Where(t => t.IsPublic ||
                               t.AllowedUsers.Any(au => au.UserId == user.Id) ||
                               t.UserId == user.Id)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToListAsync();
            }

            return await _context.Templates
                .Include(t => t.User)
                .Include(t => t.Tags)
                .Where(t => t.IsPublic)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        private async Task<List<Template>> SearchTemplates(string query)
        {
            var searchTerms = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var queryable = _context.Templates
                .Include(t => t.User)
                .Include(t => t.Tags)
                .AsQueryable();

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                if (!User.IsInRole("Admin"))
                {
                    queryable = queryable.Where(t =>
                        t.IsPublic ||
                        t.AllowedUsers.Any(au => au.UserId == user.Id) ||
                        t.UserId == user.Id);
                }
            }
            else
            {
                queryable = queryable.Where(t => t.IsPublic);
            }

            if (_fullTextSearchEnabled)
            {
                foreach (var term in searchTerms)
                {
                    var currentTerm = term;
                    queryable = queryable.Where(t =>
                        EF.Functions.Contains(t.Title, currentTerm) ||
                        EF.Functions.Contains(t.Description, currentTerm) ||
                        t.Tags.Any(tag => EF.Functions.Contains(tag.Name, currentTerm)) ||
                        t.Comments.Any(c => EF.Functions.Contains(c.Content, currentTerm)));
                }
            }
            else
            {
                foreach (var term in searchTerms)
                {
                    var currentTerm = term;
                    queryable = queryable.Where(t =>
                        t.Title.Contains(currentTerm) ||
                        t.Description.Contains(currentTerm) ||
                        t.Tags.Any(tag => tag.Name.Contains(currentTerm)) ||
                        t.Comments.Any(c => c.Content.Contains(currentTerm)));
                }
            }

            return await queryable
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
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