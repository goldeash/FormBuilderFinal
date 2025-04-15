using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FormBuilder.Models;

namespace FormBuilder.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet("check-status")]
        [Authorize]
        public async Task<IActionResult> CheckUserStatus()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.IsBlocked)
            {
                // Если пользователь заблокирован или удален
                await _signInManager.SignOutAsync();
                return Forbid();
            }

            return Ok(new
            {
                username = user.UserName,
                isAdmin = await _userManager.IsInRoleAsync(user, "Admin")
            });
        }
    }
}