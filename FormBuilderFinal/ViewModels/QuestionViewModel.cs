using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Добавьте эту строку

namespace FormBuilder.ViewModels
{
    public class QuestionViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Question text is required")]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Question type is required")]
        public string Type { get; set; }

        public int Position { get; set; }

        public bool IsRequired { get; set; }

        public bool HaveAnswer { get; set; }

        public string? CorrectAnswer { get; set; }

        public List<OptionViewModel> Options { get; set; } = new List<OptionViewModel>();
    }

    public class OptionViewModel
    {
        [Required(ErrorMessage = "Option value is required")]
        [StringLength(200, ErrorMessage = "Option cannot be longer than 200 characters")]
        public string Value { get; set; }

        public bool IsCorrect { get; set; }
    }
}