using System.Security.Claims;
using FormBuilder.Data;
using FormBuilder.Models;
using FormBuilder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Controllers
{
    public class TemplateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TemplateController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var templates = await _context.Templates
                .Where(t => t.UserId == user.Id)
                .Include(t => t.Questions)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            return View(templates);
        }

        [HttpGet]
        [Authorize]
        public IActionResult Create()
        {
            return View(new TemplateViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TemplateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound();

                var template = new Template
                {
                    Title = model.Title,
                    Description = model.Description,
                    Topic = model.Topic,
                    IsPublic = model.IsPublic,
                    UserId = user.Id,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                foreach (var tagName in model.Tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct() ?? Enumerable.Empty<string>())
                {
                    template.Tags.Add(new TemplateTag { Name = tagName });
                }

                if (!model.IsPublic)
                {
                    foreach (var email in model.AllowedUserEmails?.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct() ?? Enumerable.Empty<string>())
                    {
                        var allowedUser = await _userManager.FindByEmailAsync(email);
                        if (allowedUser != null)
                        {
                            template.AllowedUsers.Add(new TemplateAccess { UserId = allowedUser.Id });
                        }
                    }
                }

                for (int i = 0; i < model.Questions?.Count; i++)
                {
                    var questionModel = model.Questions[i];
                    if (string.IsNullOrWhiteSpace(questionModel.Title)) continue;

                    var question = new Question
                    {
                        Title = questionModel.Title,
                        Description = questionModel.Description ?? string.Empty,
                        Type = Enum.Parse<QuestionType>(questionModel.Type),
                        Position = i,
                        ShowInTable = questionModel.ShowInTable,
                        IsActive = questionModel.IsActive
                    };

                    if (question.Type == QuestionType.MultipleChoice)
                    {
                        foreach (var optionValue in questionModel.Options?.Where(o => !string.IsNullOrWhiteSpace(o)) ?? Enumerable.Empty<string>())
                        {
                            question.Options.Add(new Option { Value = optionValue });
                        }
                    }

                    template.Questions.Add(question);
                }

                _context.Templates.Add(template);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var template = await _context.Templates
                .Include(t => t.Tags)
                .Include(t => t.AllowedUsers)
                    .ThenInclude(ta => ta.User)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (template.UserId != user.Id && !User.IsInRole("Admin")) return Forbid();

            var model = new TemplateViewModel
            {
                Id = template.Id,
                Title = template.Title,
                Description = template.Description,
                Topic = template.Topic,
                IsPublic = template.IsPublic,
                Tags = template.Tags.Select(t => t.Name).ToList(),
                AllowedUserEmails = template.AllowedUsers.Select(ta => ta.User?.Email).Where(e => e != null).ToList(),
                Questions = template.Questions
                    .OrderBy(q => q.Position)
                    .Select(q => new QuestionViewModel
                    {
                        Id = q.Id,
                        Title = q.Title,
                        Description = q.Description,
                        Type = q.Type.ToString(),
                        Position = q.Position,
                        ShowInTable = q.ShowInTable,
                        IsActive = q.IsActive,
                        Options = q.Options.Select(o => o.Value).ToList()
                    }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TemplateViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var template = await _context.Templates
                    .Include(t => t.Tags)
                    .Include(t => t.AllowedUsers)
                        .ThenInclude(ta => ta.User)
                    .Include(t => t.Questions)
                        .ThenInclude(q => q.Options)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (template == null) return NotFound();

                var user = await _userManager.GetUserAsync(User);
                if (template.UserId != user.Id && !User.IsInRole("Admin")) return Forbid();

                template.Title = model.Title;
                template.Description = model.Description;
                template.Topic = model.Topic;
                template.IsPublic = model.IsPublic;
                template.UpdatedDate = DateTime.UtcNow;

                var existingTags = template.Tags.ToList();
                var newTags = model.Tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList() ?? new List<string>();

                foreach (var tag in existingTags.Where(t => !newTags.Contains(t.Name)))
                    _context.TemplateTags.Remove(tag);

                foreach (var tagName in newTags.Where(t => !existingTags.Any(et => et.Name == t)))
                    template.Tags.Add(new TemplateTag { Name = tagName });

                var existingAccesses = template.AllowedUsers.ToList();
                var newUserEmails = model.IsPublic ? new List<string>() :
                    model.AllowedUserEmails?.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList() ?? new List<string>();

                foreach (var access in existingAccesses.Where(a => !newUserEmails.Contains(a.User?.Email)))
                    _context.TemplateAccesses.Remove(access);

                if (!model.IsPublic)
                {
                    foreach (var email in newUserEmails.Where(e => !existingAccesses.Any(a => a.User?.Email == e)))
                    {
                        var allowedUser = await _userManager.FindByEmailAsync(email);
                        if (allowedUser != null)
                            template.AllowedUsers.Add(new TemplateAccess { UserId = allowedUser.Id });
                    }
                }

                var existingQuestions = template.Questions.ToList();

                var questionsToRemove = existingQuestions
                    .Where(eq => !model.Questions.Any(mq => mq.Id == eq.Id))
                    .ToList();

                foreach (var question in questionsToRemove)
                {
                    var hasAnswers = await _context.Answers.AnyAsync(a => a.QuestionId == question.Id);
                    if (hasAnswers)
                    {
                        question.IsActive = false;
                        continue;
                    }

                    _context.Options.RemoveRange(question.Options);
                    _context.Questions.Remove(question);
                }

                for (int i = 0; i < model.Questions?.Count; i++)
                {
                    var questionModel = model.Questions[i];
                    if (string.IsNullOrWhiteSpace(questionModel.Title)) continue;

                    var question = existingQuestions.FirstOrDefault(q => q.Id == questionModel.Id);

                    if (question == null)
                    {
                        question = new Question
                        {
                            TemplateId = template.Id,
                            Position = i,
                            IsActive = questionModel.IsActive
                        };
                        template.Questions.Add(question);
                    }

                    question.Title = questionModel.Title;
                    question.Description = questionModel.Description ?? string.Empty;
                    question.Type = Enum.Parse<QuestionType>(questionModel.Type);
                    question.Position = i;
                    question.ShowInTable = questionModel.ShowInTable;
                    question.IsActive = questionModel.IsActive;

                    if (question.Type == QuestionType.MultipleChoice)
                    {
                        var existingOptions = question.Options.ToList();
                        var newOptions = questionModel.Options?.Where(o => !string.IsNullOrWhiteSpace(o)).ToList() ?? new List<string>();

                        foreach (var option in existingOptions.Where(o => !newOptions.Contains(o.Value)))
                            _context.Options.Remove(option);

                        foreach (var optionValue in newOptions.Where(o => !existingOptions.Any(eo => eo.Value == o)))
                            question.Options.Add(new Option { Value = optionValue });
                    }
                    else
                    {
                        foreach (var option in question.Options.ToList())
                            _context.Options.Remove(option);
                    }
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TemplateExists(template.Id)) return NotFound();
                    throw;
                }

                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpGet]
        [Route("Template/View/{id:int}")]
        public async Task<IActionResult> ViewTemplate(int id, string tab = "details")
        {
            var template = await _context.Templates
                .Include(t => t.User)
                .Include(t => t.Tags)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .Include(t => t.AllowedUsers)
                    .ThenInclude(au => au.User)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Likes)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var isAuthorized = User.IsInRole("Admin") || template.UserId == user?.Id;
            var hasAccess = User.IsInRole("Admin") || template.IsPublic ||
                           template.AllowedUsers.Any(au => au.UserId == user?.Id) ||
                           template.UserId == user?.Id;

            ViewBag.IsAuthorized = isAuthorized;
            ViewBag.HasAccess = hasAccess;
            ViewBag.ActiveTab = tab;

            if (tab == "responses" && isAuthorized)
            {
                var responses = await _context.Forms
                    .Include(f => f.User)
                    .Where(f => f.TemplateId == id)
                    .OrderByDescending(f => f.CreatedDate)
                    .ToListAsync();

                ViewBag.Responses = responses;
            }

            return View(template);
        }   

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var template = await _context.Templates
                .Include(t => t.Tags)
                .Include(t => t.AllowedUsers)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (template.UserId != user.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var forms = await _context.Forms
                .Where(f => f.TemplateId == id)
                .Include(f => f.Answers)
                .ToListAsync();

            foreach (var form in forms)
            {
                _context.Answers.RemoveRange(form.Answers);
            }
            _context.Forms.RemoveRange(forms);

            _context.TemplateTags.RemoveRange(template.Tags);
            _context.TemplateAccesses.RemoveRange(template.AllowedUsers);

            foreach (var question in template.Questions)
            {
                _context.Options.RemoveRange(question.Options);
            }
            _context.Questions.RemoveRange(template.Questions);

            _context.Templates.Remove(template);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return RedirectToAction("ViewTemplate", new { id, error = "Failed to delete template due to database constraints" });
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddComment(int templateId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest("Comment cannot be empty");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.IsBlocked)
            {
                return Forbid();
            }

            var template = await _context.Templates.FindAsync(templateId);
            if (template == null)
            {
                return NotFound();
            }

            var comment = new Comment
            {
                Content = content,
                UserId = user.Id,
                TemplateId = templateId,
                CreatedDate = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("View", new { id = templateId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int templateId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.IsBlocked)
            {
                return Forbid();
            }

            var template = await _context.Templates.FindAsync(templateId);
            if (template == null)
            {
                return NotFound();
            }

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == user.Id && l.TemplateId == templateId);

            if (existingLike != null)
            {
                _context.Likes.Remove(existingLike);
            }
            else
            {
                var like = new Like
                {
                    UserId = user.Id,
                    TemplateId = templateId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Likes.Add(like);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("View", new { id = templateId });
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(Enumerable.Empty<string>());

            var users = await _userManager.Users
                .Where(u => u.Email.Contains(term) || u.UserName.Contains(term))
                .Take(10)
                .Select(u => u.Email)
                .ToListAsync();

            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> SearchTags(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(Enumerable.Empty<string>());

            var tags = await _context.TemplateTags
                .Where(t => t.Name.Contains(term))
                .Select(t => t.Name)
                .Distinct()
                .Take(10)
                .ToListAsync();

            return Json(tags);
        }

        private bool TemplateExists(int id)
        {
            return _context.Templates.Any(e => e.Id == id);
        }
    }
}