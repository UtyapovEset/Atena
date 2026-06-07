namespace WebAtena.Models
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Login { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Role { get; set; } = "Customer";
        public DateTime CreatedAt { get; set; }
    }
}