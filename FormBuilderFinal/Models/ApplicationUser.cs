using Microsoft.AspNetCore.Identity;

namespace FormBuilder.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsBlocked { get; set; } = false;
    }
}