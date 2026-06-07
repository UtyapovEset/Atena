using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAtena.Models;
using System.Text.Json;

namespace WebAtena.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            ViewBag.Total = cart.Sum(x => x.Price * x.Quantity);
            return View(cart);
        }

        public IActionResult Count()
        {
            var cart = GetCart();
            return Json(new { count = cart.Sum(x => x.Quantity) });
        }

        [HttpPost]
        public IActionResult Add(int productId, int quantity = 1)
        {
            if (quantity < 1) quantity = 1;

            var product = _context.Products.Find(productId);
            if (product == null)
                return Json(new { success = false, message = "Товар не найден" });

            if (product.Stock < quantity)
                return Json(new { success = false, message = "Недостаточно товара на складе" });

            var cart = GetCart();
            var existing = cart.FirstOrDefault(x => x.ProductId == productId);

            if (existing != null)
            {
                if (existing.Quantity + quantity > product.Stock)
                    return Json(new { success = false, message = "Превышено доступное количество" });

                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl
                });
            }

            SaveCart(cart);

            return Json(new
            {
                success = true,
                count = cart.Sum(x => x.Quantity),
                message = $"Добавлено {quantity} шт. {product.Name}"
            });
        }

        [HttpPost]
        public IActionResult Update(int productId, int quantity)
        {
            if (quantity < 1) return BadRequest();

            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item == null) return NotFound();

            var product = _context.Products.Find(productId);
            if (product == null || quantity > product.Stock)
                return Json(new { success = false, message = "Недостаточно на складе" });

            item.Quantity = quantity;
            SaveCart(cart);

            return Json(new { success = true });
        }

        public IActionResult Remove(int productId)
        {
            var cart = GetCart();
            cart.RemoveAll(x => x.ProductId == productId);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            HttpContext.Session.Remove("cart");
            return RedirectToAction("Index");
        }

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString("cart");
            return string.IsNullOrEmpty(json)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("cart", JsonSerializer.Serialize(cart));
        }
    }
}