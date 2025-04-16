using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FormBuilder.ViewModels
{
    public class TemplateViewModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        public string Topic { get; set; } = "Other";

        public bool IsPublic { get; set; } = true;

        public List<string> Tags { get; set; } = new List<string>();
        public List<string> AllowedUserEmails { get; set; } = new List<string>();
        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
    }
}