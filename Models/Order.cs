using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebAtena.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(200, MinimumLength = 2)]
        [Display(Name = "Имя клиента")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефон обязателен")]
        [StringLength(20)]
        [Phone]
        [Display(Name = "Телефон")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(1000)]
        [Display(Name = "Комментарий")]
        public string? Comment { get; set; }

        [StringLength(50)]
        [Display(Name = "Статус")]
        public string Status { get; set; } = "Новый";

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(50)]
        [Display(Name = "Номер заказа")]
        public string OrderNumber { get; set; } = string.Empty;

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}