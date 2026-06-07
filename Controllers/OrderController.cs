using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAtena.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace WebAtena.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private List<CartItem> GetCartFromSession()
        {
            var json = HttpContext.Session.GetString("cart");
            return string.IsNullOrEmpty(json)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }

        public async Task<IActionResult> Create()
        {
            var cart = GetCartFromSession();
            if (!cart.Any())
            {
                TempData["Error"] = "Корзина пуста";
                return RedirectToAction("Index", "Cart");
            }

            var order = new Order();

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    order.CustomerName = user.FullName ?? "";
                    order.Phone = user.PhoneNumber ?? "";
                    order.Email = user.Email;
                    order.UserId = user.Id;
                }
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            var cart = GetCartFromSession();
            if (!cart.Any())
            {
                TempData["Error"] = "Корзина пуста";
                return RedirectToAction("Index", "Cart");
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    order.UserId = user.Id;
                    order.CustomerName = string.IsNullOrWhiteSpace(order.CustomerName)
                        ? (user.FullName ?? user.UserName ?? "Клиент")
                        : order.CustomerName;
                    order.Phone = string.IsNullOrWhiteSpace(order.Phone)
                        ? (user.PhoneNumber ?? "")
                        : order.Phone;
                    order.Email = string.IsNullOrWhiteSpace(order.Email)
                        ? user.Email
                        : order.Email;
                }
            }

            foreach (var item in cart)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                {
                    TempData["Error"] = $"Недостаточно товара «{item.ProductName}» на складе";
                    return RedirectToAction("Index", "Cart");
                }
            }

            order.CreatedAt = DateTime.Now;
            order.Status = "Новый";
            order.OrderNumber = GenerateUniqueOrderNumber();

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();   

            decimal totalSum = 0;

            foreach (var cartItem in cart)
            {
                var product = await _context.Products.FindAsync(cartItem.ProductId);
                if (product != null)
                {
                    product.Stock -= cartItem.Quantity;

                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Price
                    });

                    totalSum += cartItem.Price * cartItem.Quantity;
                }
            }

            await _context.SaveChangesAsync();

            var userIdStr = _userManager.GetUserId(User);
            int userId = userIdStr != null && int.TryParse(userIdStr, out int uid) ? uid : 0;

            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                UserName = User.Identity?.Name ?? "Гость",
                Action = "CREATE_ORDER",
                Description = $"Оформлен заказ #{order.OrderNumber} на сумму {totalSum:N0} ₽",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("cart");

            TempData["Success"] = $"Заказ #{order.OrderNumber} успешно оформлен!";

            return RedirectToAction("Success", "Order", new { id = order.Id });
        }

        private string GenerateUniqueOrderNumber()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            while (true)
            {
                var number = new string(Enumerable.Repeat(chars, 6)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                if (!_context.Orders.Any(o => o.OrderNumber == number))
                    return number;
            }
        }

        public async Task<IActionResult> Success(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return RedirectToAction("Index", "Home");

            return View(order);
        }
    }
}