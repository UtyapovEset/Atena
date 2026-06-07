using System.ComponentModel.DataAnnotations;

namespace WebAtena.ViewModels
{
    public class ProfileViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Логин")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Полное имя")]
        public string? FullName { get; set; }

        [EmailAddress(ErrorMessage = "Введите корректный email")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone]
        [Display(Name = "Телефон")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Роль")]
        public string Role { get; set; } = "Customer";

        public bool IsAdminOrEmployee { get; set; } = false;
    }
}