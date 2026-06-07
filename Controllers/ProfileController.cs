using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAtena.Models;
using WebAtena.ViewModels;

namespace WebAtena.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var model = new ProfileViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsAdminOrEmployee = user.Role == "Admin" || user.Role == "Employee"
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Update(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            if (user.Role == "Admin" || user.Role == "Employee")
            {
                if (!string.IsNullOrWhiteSpace(model.UserName) && model.UserName != user.UserName)
                {
                    var existing = await _userManager.FindByNameAsync(model.UserName);
                    if (existing != null && existing.Id != user.Id)
                    {
                        TempData["Error"] = "Логин уже занят";
                        return RedirectToAction("Index");
                    }
                    user.UserName = model.UserName;
                }
            }

            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Данные успешно обновлены";

            return RedirectToAction("Index");
        }

        public IActionResult ChangePassword() => View();

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = "Пароль успешно изменён";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Ошибка при смене пароля";
            return View();
        }

        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);

            // Загружаем заказ вместе с позициями (Items)
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null)
            {
                TempData["Error"] = "Заказ не найден";
                return RedirectToAction("MyOrders");
            }

            if (order.Status != "Новый")
            {
                TempData["Error"] = "Можно отменить только новый заказ";
                return RedirectToAction("MyOrders");
            }

            order.Status = "Отменен";

            // Возврат количества товара на склад
            foreach (var item in order.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock += item.Quantity;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Заказ #{order.OrderNumber} отменен";
            return RedirectToAction("MyOrders");
        }
    }
}