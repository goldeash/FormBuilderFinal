// Models/Form.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormBuilder.Models
{
    public class Form
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TemplateId { get; set; }

        [ForeignKey("TemplateId")]
        public virtual Template Template { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}