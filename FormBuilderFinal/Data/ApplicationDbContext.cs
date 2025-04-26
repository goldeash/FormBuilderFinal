// Data/ApplicationDbContext.cs
using FormBuilder.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Template> Templates { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<TemplateTag> TemplateTags { get; set; }
        public DbSet<TemplateAccess> TemplateAccesses { get; set; }
        public DbSet<Form> Forms { get; set; }
        public DbSet<Answer> Answers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
                .Property(u => u.IsBlocked)
                .HasDefaultValue(false);

            // Configure relationships
            builder.Entity<Template>()
                .HasMany(t => t.Questions)
                .WithOne(q => q.Template)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Template>()
                .HasMany(t => t.Tags)
                .WithOne(tt => tt.Template)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Template>()
                .HasMany(t => t.AllowedUsers)
                .WithOne(ta => ta.Template)
                .OnDelete(DeleteBehavior.ClientCascade);

            builder.Entity<Question>()
                .HasMany(q => q.Options)
                .WithOne(o => o.Question)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TemplateAccess>()
                .HasOne(ta => ta.User)
                .WithMany()
                .OnDelete(DeleteBehavior.ClientCascade);

            builder.Entity<Form>()
                .HasOne(f => f.Template)
                .WithMany()
                .OnDelete(DeleteBehavior.ClientCascade);

            builder.Entity<Form>()
                .HasOne(f => f.User)
                .WithMany()
                .OnDelete(DeleteBehavior.ClientCascade);

            builder.Entity<Answer>()
                .HasOne(a => a.Form)
                .WithMany(f => f.Answers)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany()
                .OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}