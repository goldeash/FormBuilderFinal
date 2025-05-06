using FormBuilder.Data;
using FormBuilder.Models;
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
        private const int PageSize = 10;

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

        public async Task<IActionResult> Index(int page = 0, string tab = "new")
        {
            ViewBag.ActiveTab = tab;
            ViewBag.CurrentPage = page;

            var templates = await GetTemplatesForCurrentUser(page, tab);
            ViewBag.HasMore = await CheckHasMore(page, tab);

            return View(templates);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query, int page = 0, string tab = "new")
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index");
            }

            ViewBag.SearchQuery = query;
            ViewBag.ActiveTab = tab;
            ViewBag.CurrentPage = page;

            var templates = await SearchTemplates(query, page, tab);
            ViewBag.HasMore = await CheckHasMore(page, tab, query);

            return View("Index", templates);
        }

        [HttpGet]
        public async Task<IActionResult> LoadMore(int page, string tab, string query = null)
        {
            List<Template> templates;
            if (!string.IsNullOrEmpty(query))
            {
                templates = await SearchTemplates(query, page, tab);
            }
            else
            {
                templates = await GetTemplatesForCurrentUser(page, tab);
            }

            var hasMore = await CheckHasMore(page, tab, query);

            return Json(new
            {
                templates = templates.Select(t => new {
                    id = t.Id,
                    title = t.Title,
                    description = t.Description,
                    userEmail = t.User.Email,
                    createdDate = t.CreatedDate.ToString("g"),
                    likesCount = t.Likes.Count,
                    topic = t.Topic,
                    url = Url.Action("ViewTemplate", "Template", new { id = t.Id })
                }),
                hasMore
            });
        }

        private async Task<bool> CheckHasMore(int page, string tab, string query = null)
        {
            IQueryable<Template> baseQuery;
            if (!string.IsNullOrEmpty(query))
            {
                baseQuery = await BuildSearchQuery(query);
            }
            else
            {
                baseQuery = await BuildBaseQuery();
            }

            baseQuery = ApplySorting(baseQuery, tab);

            return await baseQuery.CountAsync() > (page + 1) * PageSize;
        }

        private async Task<List<Template>> GetTemplatesForCurrentUser(int page, string tab)
        {
            var query = await BuildBaseQuery();
            query = ApplySorting(query, tab);

            return await query
                .Include(t => t.Likes)
                .Skip(page * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        private async Task<List<Template>> SearchTemplates(string query, int page, string tab)
        {
            var searchTerms = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var queryable = await BuildSearchQuery(query);
            queryable = ApplySorting(queryable, tab);

            return await queryable
                .Include(t => t.Likes)
                .Skip(page * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        private async Task<IQueryable<Template>> BuildBaseQuery()
        {
            var query = _context.Templates
                .Include(t => t.User)
                .Include(t => t.Tags)
                .AsQueryable();

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                if (User.IsInRole("Admin"))
                {
                    return query;
                }

                query = query.Where(t => t.IsPublic ||
                                       t.AllowedUsers.Any(au => au.UserId == user.Id) ||
                                       t.UserId == user.Id);
            }
            else
            {
                query = query.Where(t => t.IsPublic);
            }

            return query;
        }

        private async Task<IQueryable<Template>> BuildSearchQuery(string query)
        {
            var searchTerms = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var queryable = await BuildBaseQuery();

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

            return queryable;
        }

        private IQueryable<Template> ApplySorting(IQueryable<Template> query, string tab)
        {
            return tab == "popular"
                ? query.OrderByDescending(t => t.Likes.Count)
                : query.OrderByDescending(t => t.CreatedDate);
        }
    }
}