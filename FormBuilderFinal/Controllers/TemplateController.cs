using System;
using System.Linq;
using System.Threading.Tasks;
using FormBuilder.Data;
using FormBuilder.Models;
using FormBuilder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Controllers
{
    [Authorize]
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
        public IActionResult Create()
        {
            return View(new TemplateViewModel());
        }

        [HttpPost]
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

                // Add tags
                foreach (var tagName in model.Tags.Distinct())
                {
                    template.Tags.Add(new TemplateTag { Name = tagName });
                }

                // Add allowed users if template is not public
                if (!model.IsPublic)
                {
                    foreach (var email in model.AllowedUserEmails.Distinct())
                    {
                        var allowedUser = await _userManager.FindByEmailAsync(email);
                        if (allowedUser != null)
                        {
                            template.AllowedUsers.Add(new TemplateAccess { UserId = allowedUser.Id });
                        }
                    }
                }

                // Add questions
                for (int i = 0; i < model.Questions.Count; i++)
                {
                    var questionModel = model.Questions[i];
                    var question = new Question
                    {
                        Title = questionModel.Title,
                        Description = questionModel.Description ?? string.Empty,
                        Type = Enum.Parse<QuestionType>(questionModel.Type),
                        Position = i,
                        IsRequired = questionModel.IsRequired,
                        HaveAnswer = questionModel.HaveAnswer,
                        CorrectAnswer = questionModel.CorrectAnswer
                    };

                    // Add options for multiple choice questions
                    if (question.Type == QuestionType.MultipleChoice)
                    {
                        foreach (var option in questionModel.Options)
                        {
                            question.Options.Add(new Option
                            {
                                Value = option.Value,
                                IsCorrect = questionModel.HaveAnswer && option.IsCorrect
                            });
                        }
                    }

                    template.Questions.Add(question);
                }

                _context.Templates.Add(template);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            // Add validation errors to ModelState
            foreach (var question in model.Questions)
            {
                for (int i = 0; i < question.Options.Count; i++)
                {
                    var option = question.Options[i];
                    if (string.IsNullOrWhiteSpace(option.Value))
                    {
                        ModelState.AddModelError($"Questions[{question.Position}].Options[{i}].Value", "Option value is required");
                    }
                }
            }

            return View(model);
        }

        [HttpGet]
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
                AllowedUserEmails = template.AllowedUsers.Select(ta => ta.User.Email).ToList(),
                Questions = template.Questions
                    .OrderBy(q => q.Position)
                    .Select(q => new QuestionViewModel
                    {
                        Id = q.Id,
                        Title = q.Title,
                        Description = q.Description,
                        Type = q.Type.ToString(),
                        Position = q.Position,
                        IsRequired = q.IsRequired,
                        HaveAnswer = q.HaveAnswer,
                        CorrectAnswer = q.CorrectAnswer,
                        Options = q.Options.Select(o => new OptionViewModel
                        {
                            Value = o.Value,
                            IsCorrect = o.IsCorrect
                        }).ToList()
                    }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TemplateViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var template = await _context.Templates
                    .Include(t => t.Tags)
                    .Include(t => t.AllowedUsers)
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

                // Update tags
                var existingTags = template.Tags.ToList();
                var newTags = model.Tags.Distinct().ToList();

                // Remove tags not in new list
                foreach (var tag in existingTags.Where(t => !newTags.Contains(t.Name)))
                    _context.TemplateTags.Remove(tag);

                // Add new tags
                foreach (var tagName in newTags.Where(t => !existingTags.Any(et => et.Name == t)))
                    template.Tags.Add(new TemplateTag { Name = tagName });

                // Update allowed users if template is not public
                var existingAccesses = template.AllowedUsers.ToList();
                var newUserEmails = model.IsPublic ? new List<string>() : model.AllowedUserEmails.Distinct().ToList();

                // Remove accesses not in new list
                foreach (var access in existingAccesses.Where(a => !newUserEmails.Contains(a.User.Email)))
                    _context.TemplateAccesses.Remove(access);

                // Add new accesses
                if (!model.IsPublic)
                {
                    foreach (var email in newUserEmails.Where(e => !existingAccesses.Any(a => a.User.Email == e)))
                    {
                        var allowedUser = await _userManager.FindByEmailAsync(email);
                        if (allowedUser != null)
                            template.AllowedUsers.Add(new TemplateAccess { UserId = allowedUser.Id });
                    }
                }

                // Update questions
                var existingQuestions = template.Questions.ToList();

                // Remove questions not in new list
                foreach (var question in existingQuestions.Where(q => !model.Questions.Any(mq => mq.Id == q.Id)))
                    _context.Questions.Remove(question);

                // Update or add questions
                for (int i = 0; i < model.Questions.Count; i++)
                {
                    var questionModel = model.Questions[i];
                    var question = existingQuestions.FirstOrDefault(q => q.Id == questionModel.Id);

                    if (question == null)
                    {
                        question = new Question
                        {
                            TemplateId = template.Id,
                            Position = i,
                            IsRequired = questionModel.IsRequired,
                            HaveAnswer = questionModel.HaveAnswer,
                            CorrectAnswer = questionModel.CorrectAnswer
                        };
                        template.Questions.Add(question);
                    }

                    question.Title = questionModel.Title;
                    question.Description = questionModel.Description ?? string.Empty;
                    question.Type = Enum.Parse<QuestionType>(questionModel.Type);
                    question.Position = i;
                    question.IsRequired = questionModel.IsRequired;
                    question.HaveAnswer = questionModel.HaveAnswer;
                    question.CorrectAnswer = questionModel.CorrectAnswer;

                    // Update options for multiple choice questions
                    if (question.Type == QuestionType.MultipleChoice)
                    {
                        var existingOptions = question.Options.ToList();
                        var newOptions = questionModel.Options ?? new List<OptionViewModel>();

                        // Remove options not in new list
                        foreach (var option in existingOptions.Where(o => !newOptions.Any(no => no.Value == o.Value)))
                            _context.Options.Remove(option);

                        // Add new options and update existing ones
                        foreach (var optionModel in newOptions)
                        {
                            var option = existingOptions.FirstOrDefault(o => o.Value == optionModel.Value);
                            if (option == null)
                            {
                                option = new Option
                                {
                                    Value = optionModel.Value,
                                    IsCorrect = questionModel.HaveAnswer && optionModel.IsCorrect
                                };
                                question.Options.Add(option);
                            }
                            else
                            {
                                option.IsCorrect = questionModel.HaveAnswer && optionModel.IsCorrect;
                            }
                        }
                    }
                    else
                    {
                        // Remove all options if question type changed
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

            // Add validation errors to ModelState
            foreach (var question in model.Questions)
            {
                for (int i = 0; i < question.Options.Count; i++)
                {
                    var option = question.Options[i];
                    if (string.IsNullOrWhiteSpace(option.Value))
                    {
                        ModelState.AddModelError($"Questions[{question.Position}].Options[{i}].Value", "Option value is required");
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var template = await _context.Templates.FindAsync(id);
            if (template == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (template.UserId != user.Id && !User.IsInRole("Admin")) return Forbid();

            _context.Templates.Remove(template);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term)
        {
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