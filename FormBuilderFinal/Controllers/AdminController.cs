using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormBuilder.Models;
using FormBuilder.Data;
using FormBuilderFinal.ViewModels;

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

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var templates = await _context.Templates
                    .Where(t => t.UserId == user.Id)
                    .ToListAsync();

                _context.Templates.RemoveRange(templates);

                var forms = await _context.Forms
                    .Where(f => f.UserId == user.Id)
                    .ToListAsync();

                _context.Forms.RemoveRange(forms);

                await _context.Comments
                    .Where(c => c.UserId == user.Id)
                    .ExecuteDeleteAsync();

                await _context.Likes
                    .Where(l => l.UserId == user.Id)
                    .ExecuteDeleteAsync();

                await _context.TemplateAccesses
                    .Where(ta => ta.UserId == user.Id)
                    .ExecuteDeleteAsync();

                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("Failed to delete user");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (isCurrentUser)
                {
                    await _signInManager.SignOutAsync();
                    return RedirectToAction("Index", "Home");
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "An error occurred while deleting the user");
            }
        }
    }
}   