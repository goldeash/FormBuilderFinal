using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilder.Models
{
    public class Template
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        public string Topic { get; set; } = "Other";

        public bool IsPublic { get; set; } = true;

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<TemplateTag> Tags { get; set; } = new List<TemplateTag>();
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<TemplateAccess> AllowedUsers { get; set; } = new List<TemplateAccess>();
    }
}