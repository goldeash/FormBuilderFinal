﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilder.Models
{
    public enum QuestionType
    {
        SingleLineText,
        MultiLineText,
        Integer,
        MultipleChoice
    }

    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public QuestionType Type { get; set; }

        public int Position { get; set; }

        public bool ShowInTable { get; set; }

        [Required]
        public int TemplateId { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey("TemplateId")]
        public virtual Template Template { get; set; }

        public virtual ICollection<Option> Options { get; set; } = new List<Option>();
    }
}