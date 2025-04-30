using FormBuilder.Data;
using FormBuilder.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Controllers
{
    [Authorize]
    public class FormController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FormController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Fill(int templateId)
        {
            var template = await _context.Templates
                .Include(t => t.Questions
                    .Where(q => q.IsActive))
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.IsBlocked)
            {
                return Forbid();
            }

            if (!template.IsPublic &&
                !template.AllowedUsers.Any(au => au.UserId == user.Id) &&
                template.UserId != user.Id)
            {
                return Forbid();
            }

            return View(template);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fill(int templateId, FormInputModel model)
        {
            var template = await _context.Templates
                .Include(t => t.Questions
                    .Where(q => q.IsActive))
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.IsBlocked)
            {
                return Forbid();
            }

            if (!template.IsPublic &&
                !template.AllowedUsers.Any(au => au.UserId == user.Id) &&
                template.UserId != user.Id)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var form = new Form
                {
                    TemplateId = templateId,
                    UserId = user.Id,
                    CreatedDate = DateTime.UtcNow
                };

                foreach (var answer in model.Answers)
                {
                    var question = template.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                    if (question == null) continue;

                    form.Answers.Add(new Answer
                    {
                        QuestionId = question.Id,
                        Value = answer.Value
                    });
                }

                _context.Forms.Add(form);
                await _context.SaveChangesAsync();

                return RedirectToAction("View", "Template", new { id = templateId });
            }

            template = await _context.Templates
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            return View(template);
        }

        [HttpGet]
        [Route("Form/View/{id:int}")]
        public async Task<IActionResult> ViewForm(int id)
        {
            var form = await _context.Forms
                .Include(f => f.Template)
                .Include(f => f.User)
                .Include(f => f.Answers)
                    .ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.IsBlocked)
            {
                return Forbid();
            }

            if (!User.IsInRole("Admin") &&
                form.UserId != user.Id &&
                form.Template.UserId != user.Id)
            {
                return Forbid();
            }

            return View(form);
        }

        public class FormInputModel
        {
            public List<AnswerInputModel> Answers { get; set; } = new List<AnswerInputModel>();
        }

        public class AnswerInputModel
        {
            public int QuestionId { get; set; }
            public string Value { get; set; }
        }
    }
}