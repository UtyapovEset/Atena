using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAtena.Models;

namespace WebAtena.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public EmployeeController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        private string? GetRole() => HttpContext.Session.GetString("role");
        private int? GetUserId() => HttpContext.Session.GetInt32("userId");
        private string? GetUserName() => HttpContext.Session.GetString("username");
        private bool IsEmployee() => GetRole() == "Employee" || GetRole() == "Admin";

        public IActionResult Dashboard()
        {
            if (!IsEmployee()) return Redirect("/Account/Login");

            var ordersCount = _context.Orders.Count();
            var productsCount = _context.Products.Count();
            var revenue = _context.Orders
                .Where(o => o.Status == "Выполнен")
                .Include(o => o.Items)
                .ToList()
                .Sum(o => o.Items.Sum(i => i.Price * i.Quantity));
            var activeOrders = _context.Orders.Count(o => o.Status == "Новый" || o.Status == "В обработке");
            var lowStockProducts = _context.Products.Count(p => p.Stock <= 5 && p.Stock > 0);

            ViewBag.Orders = ordersCount;
            ViewBag.Products = productsCount;
            ViewBag.TotalRevenue = revenue;
            ViewBag.ActiveOrders = activeOrders;
            ViewBag.LowStockProducts = lowStockProducts;

            return View();
        }

        public IActionResult Products()
        {
            if (!IsEmployee()) return Redirect("/Account/Login");

            var products = _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .ToList();

            return View(products);
        }

        public IActionResult CreateProduct()
        {
            if (!IsEmployee()) return Redirect("/Account/Login");

            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product, IFormFile? image)
        {
            if (!IsEmployee()) return Unauthorized();

            try
            {
                if (image != null && image.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    product.ImageUrl = "/images/products/" + uniqueFileName;
                }

                product.CreatedAt = DateTime.Now;
                _context.Products.Add(product);

                _context.ActionLogs.Add(new ActionLog
                {
                    UserId = GetUserId() ?? 1,
                    UserName = GetUserName(),
                    Action = "CREATE_PRODUCT",
                    Description = $"Добавлен товар: {product.Name} (Количество: {product.Stock})",
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();

                TempData["Success"] = "Товар успешно добавлен!";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка: {ex.Message}";
                ViewBag.Categories = _context.Categories.ToList();
                return View(product);
            }
        }

        public IActionResult EditProduct(int id)
        {
            if (!IsEmployee()) return Redirect("/Account/Login");

            var product = _context.Products.Find(id);
            if (product == null)
            {
                TempData["Error"] = "Товар не найден";
                return RedirectToAction("Products");
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product product, IFormFile? image)
        {
            if (!IsEmployee()) return Unauthorized();

            try
            {
                var existing = await _context.Products.FindAsync(product.Id);
                if (existing == null)
                {
                    TempData["Error"] = "Товар не найден";
                    return RedirectToAction("Products");
                }

                existing.Name = product.Name;
                existing.ShortDescription = product.ShortDescription;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.CategoryId = product.CategoryId;
                existing.Stock = product.Stock;

                if (image != null && image.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    existing.ImageUrl = "/images/products/" + uniqueFileName;
                }

                _context.ActionLogs.Add(new ActionLog
                {
                    UserId = GetUserId() ?? 1,
                    UserName = GetUserName(),
                    Action = "EDIT_PRODUCT",
                    Description = $"Изменен товар: {existing.Name} (Новое количество: {existing.Stock})",
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();

                TempData["Success"] = "Товар успешно обновлен!";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка: {ex.Message}";
                ViewBag.Categories = _context.Categories.ToList();
                return View(product);
            }
        }


        public IActionResult Orders(string search)
        {
            if (!IsEmployee()) return Redirect("/Account/Login");

            var query = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.OrderNumber.Contains(search) || o.Id.ToString().Contains(search));
            }

            var orders = query.ToList();
            ViewBag.SearchTerm = search;
            return View(orders);
        }

        public IActionResult OrderDetails(int id)
        {
            if (!IsEmployee()) return Redirect("/Account/Login");

            var order = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Заказ не найден";
                return RedirectToAction("Orders");
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            if (!IsEmployee()) return Unauthorized();

            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order != null)
                {
                    order.Status = status;
                    await _context.SaveChangesAsync();

                    _context.ActionLogs.Add(new ActionLog
                    {
                        UserId = GetUserId() ?? 1,
                        UserName = GetUserName(),
                        Action = "UPDATE_ORDER_STATUS",
                        Description = $"Заказ #{order.OrderNumber} статус: {status}",
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Статус заказа обновлен";
                }
                return RedirectToAction("Orders");
            }
            catch
            {
                TempData["Error"] = "Ошибка при обновлении статуса";
                return RedirectToAction("Orders");
            }
        }
    }
}