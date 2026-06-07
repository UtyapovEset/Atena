using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAtena.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название товара обязательно")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 200 символов")]
        [Display(Name = "Название")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Краткое описание не более 500 символов")]
        [Display(Name = "Краткое описание")]
        public string? ShortDescription { get; set; }

        [Display(Name = "Полное описание")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Цена обязательна")]
        [Range(0.01, 1000000, ErrorMessage = "Цена должна быть от 0.01 до 1,000,000")]
        [Display(Name = "Цена")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Display(Name = "Изображение")]
        public string? ImageUrl { get; set; }

        // КОЛИЧЕСТВО НА СКЛАДЕ
        [Display(Name = "Количество на складе")]
        [Range(0, 10000, ErrorMessage = "Количество должно быть от 0 до 10000")]
        public int Stock { get; set; } = 0;

        [Display(Name = "В наличии")]
        public bool InStock => Stock > 0;

        [Required(ErrorMessage = "Категория обязательна")]
        [Display(Name = "Категория")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [Display(Name = "Дата добавления")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}