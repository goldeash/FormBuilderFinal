using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FormBuilder.ViewModels
{
    public class QuestionViewModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string Type { get; set; }

        public int Position { get; set; }

        public bool IsRequired { get; set; }

        public bool HaveAnswer { get; set; }

        public string? CorrectAnswer { get; set; }

        public List<OptionViewModel> Options { get; set; } = new List<OptionViewModel>();
    }

    public class OptionViewModel
    {
        public string Value { get; set; }
        public bool IsCorrect { get; set; }
    }
}