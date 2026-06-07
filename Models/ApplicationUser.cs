using Microsoft.AspNetCore.Identity;

namespace WebAtena.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string Role { get; set; } = "Customer";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}