using System.ComponentModel.DataAnnotations;

namespace WebAtena.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Логин обязателен")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 50 символов")]
        [Display(Name = "Логин")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "ФИО")]
        public string? FullName { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Введите корректный email")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(50)]
        [Display(Name = "Роль")]
        public string Role { get; set; } = "Employee";

        [Display(Name = "Дата регистрации")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}