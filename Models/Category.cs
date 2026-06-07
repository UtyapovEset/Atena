using System.ComponentModel.DataAnnotations;

namespace WebAtena.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Название категории обязательно")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 100 символов")]
        [Display(Name = "Название")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        // Навигационное свойство
        public virtual ICollection<Product>? Products { get; set; }
    }
}