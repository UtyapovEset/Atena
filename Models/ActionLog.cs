using System.ComponentModel.DataAnnotations;

namespace WebAtena.Models
{
    public class ActionLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [StringLength(100)]
        [Display(Name = "Пользователь")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Действие обязательно")]
        [StringLength(100)]
        [Display(Name = "Действие")]
        public string Action { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Дата")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}