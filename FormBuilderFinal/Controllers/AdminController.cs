using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormBuilder.Models;
using FormBuilder.Data;

namespace FormBuilder.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    IsAdmin = await _userManager.IsInRoleAsync(user, "Admin"),
                    IsBlocked = user.IsBlocked,
                    IsCurrentUser = user.Id == currentUserId
                });
            }

            return View(userViewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var isCurrentUser = user.Id == _userManager.GetUserId(User);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            await _userManager.UpdateSecurityStampAsync(user);

            if (isCurrentUser)
            {
                await _signInManager.RefreshSignInAsync(user);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var isCurrentUser = user.Id == _userManager.GetUserId(User);

            user.IsBlocked = !user.IsBlocked;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);

            if (isCurrentUser && user.IsBlocked)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var isCurrentUser = user.Id == _userManager.GetUserId(User);

            var userTemplates = await _context.Templates
                .Include(t => t.Tags)
                .Include(t => t.AllowedUsers)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .Include(t => t.Comments)
                .Include(t => t.Likes)
                .Where(t => t.UserId == user.Id)
                .ToListAsync();

            foreach (var template in userTemplates)
            {
                var forms = await _context.Forms
                    .Include(f => f.Answers)
                    .Where(f => f.TemplateId == template.Id)
                    .ToListAsync();

                foreach (var form in forms)
                {
                    _context.Answers.RemoveRange(form.Answers);
                    _context.Forms.Remove(form);
                }

                _context.TemplateTags.RemoveRange(template.Tags);
                _context.TemplateAccesses.RemoveRange(template.AllowedUsers);
                _context.Comments.RemoveRange(template.Comments);
                _context.Likes.RemoveRange(template.Likes);

                foreach (var question in template.Questions)
                {
                    _context.Options.RemoveRange(question.Options);
                }
                _context.Questions.RemoveRange(template.Questions);
                _context.Templates.Remove(template);
            }

            var userForms = await _context.Forms
                .Include(f => f.Answers)
                .Where(f => f.UserId == user.Id)
                .ToListAsync();

            foreach (var form in userForms)
            {
                _context.Answers.RemoveRange(form.Answers);
                _context.Forms.Remove(form);
            }

            var userLikes = await _context.Likes
                .Where(l => l.UserId == user.Id)
                .ToListAsync();
            _context.Likes.RemoveRange(userLikes);

            var userComments = await _context.Comments
                .Where(c => c.UserId == user.Id)
                .ToListAsync();
            _context.Comments.RemoveRange(userComments);

            var templateAccesses = await _context.TemplateAccesses
                .Where(ta => ta.UserId == user.Id)
                .ToListAsync();
            _context.TemplateAccesses.RemoveRange(templateAccesses);

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded && isCurrentUser)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index");
        }

        public class UserViewModel
        {
            public string Id { get; set; }
            public string Email { get; set; }
            public bool IsAdmin { get; set; }
            public bool IsBlocked { get; set; }
            public bool IsCurrentUser { get; set; }
        }
    }
}   