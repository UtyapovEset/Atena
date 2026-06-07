using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAtena.Models;
using Microsoft.AspNetCore.Identity;

namespace WebAtena.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
        }

        private string? GetRole() => HttpContext.Session.GetString("role");
        private int? GetUserId() => HttpContext.Session.GetInt32("userId");
        private string? GetUserName() => HttpContext.Session.GetString("username");

        private bool IsAdmin() => GetRole() == "Admin";
        private bool IsEmployee() => GetRole() == "Employee" || GetRole() == "Admin";

        public IActionResult Dashboard()
        {
            if (!IsEmployee()) return Redirect("/Account/Login");

            var ordersCount = _context.Orders.Count();
            var productsCount = _context.Products.Count();
            var categoriesCount = _context.Categories.Count();
            var revenue = _context.Orders
                .Where(o => o.Status == "Выполнен")
                .Include(o => o.Items)
                .ToList()
                .Sum(o => o.Items.Sum(i => i.Price * i.Quantity));

            var activeOrders = _context.Orders.Count(o => o.Status == "Новый" || o.Status == "В обработке");
            var lowStockProducts = _context.Products.Count(p => p.Stock <= 5 && p.Stock > 0);

            ViewBag.Orders = ordersCount;
            ViewBag.Products = productsCount;
            ViewBag.Categories = categoriesCount;
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

            ViewBag.IsAdmin = IsAdmin();
            return View(products);
        }

        public IActionResult CreateProduct()
        {
            if (!IsAdmin()) return Unauthorized();
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
            if (!IsAdmin()) return Redirect("/Account/Login");

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product != null)
                {
                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();

                    _context.ActionLogs.Add(new ActionLog
                    {
                        UserId = GetUserId() ?? 1,
                        UserName = GetUserName(),
                        Action = "DELETE_PRODUCT",
                        Description = $"Удален товар: {product.Name}",
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Товар удален";
                }
                return RedirectToAction("Products");
            }
            catch
            {
                TempData["Error"] = "Нельзя удалить товар, так как он есть в заказах";
                return RedirectToAction("Products");
            }
        }

        public IActionResult Orders(string search)
        {
            if (!IsAdmin()) return Redirect("/Account/Login");

            var query = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                if (int.TryParse(search.Trim(), out int id))
                    query = query.Where(o => o.Id == id);
                else
                    query = query.Where(o => o.OrderNumber.Contains(search) || o.CustomerName.Contains(search));
            }

            var orders = query.ToList();
            ViewBag.SearchTerm = search;
            return View(orders);
        }

        public IActionResult OrderDetails(int id)
        {
            if (!IsAdmin()) return Redirect("/Account/Login");

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
            if (!IsAdmin()) return Unauthorized();

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
                        Description = $"Заказ #{id} статус: {status}",
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

        public IActionResult Categories()
        {
            if (!IsAdmin()) return Redirect("/Account/Login");

            var categories = _context.Categories
                .Include(c => c.Products)
                .ToList();

            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            if (!IsAdmin()) return Unauthorized();

            try
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                _context.ActionLogs.Add(new ActionLog
                {
                    UserId = GetUserId() ?? 1,
                    UserName = GetUserName(),
                    Action = "CREATE_CATEGORY",
                    Description = $"Добавлена категория: {category.Name}",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = "Категория добавлена";
                return RedirectToAction("Categories");
            }
            catch
            {
                TempData["Error"] = "Ошибка при добавлении категории";
                return RedirectToAction("Categories");
            }
        }

        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category != null)
                {
                    _context.Categories.Remove(category);
                    await _context.SaveChangesAsync();

                    _context.ActionLogs.Add(new ActionLog
                    {
                        UserId = GetUserId() ?? 1,
                        UserName = GetUserName(),
                        Action = "DELETE_CATEGORY",
                        Description = $"Удалена категория: {category.Name}",
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Категория удалена";
                }
                return RedirectToAction("Categories");
            }
            catch
            {
                TempData["Error"] = "Нельзя удалить категорию, в которой есть товары";
                return RedirectToAction("Categories");
            }
        }

        public IActionResult Analytics()
        {
            if (!IsAdmin()) return Redirect("/Account/Login");
            return View();
        }

        [HttpGet]
        public IActionResult GetAnalyticsData()
        {
            if (!IsAdmin()) return Unauthorized();

            var orders = _context.Orders
                .Where(o => o.Status == "Выполнен")
                .Include(o => o.Items)
                .ToList();

            var dailyStats = orders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    date = g.Key,
                    revenue = g.Sum(o => o.Items.Sum(i => i.Price * i.Quantity)),
                    orderCount = g.Count()
                })
                .OrderBy(d => d.date)
                .Take(30)
                .Select(d => new
                {
                    date = d.date.ToString("dd.MM.yyyy"),
                    revenue = d.revenue,
                    orderCount = d.orderCount
                })
                .ToList();

            var monthlyStats = orders
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    year = g.Key.Year,
                    month = g.Key.Month,
                    revenue = g.Sum(o => o.Items.Sum(i => i.Price * i.Quantity))
                })
                .OrderBy(d => d.year)
                .ThenBy(d => d.month)
                .Select(d => new
                {
                    month = $"{d.month:D2}.{d.year}",
                    revenue = d.revenue
                })
                .ToList();

            var totalRevenue = orders.Sum(o => o.Items.Sum(i => i.Price * i.Quantity));
            var ordersCount = _context.Orders.Count();
            var productsCount = _context.Products.Count();

            return Json(new { daily = dailyStats, monthly = monthlyStats, totalRevenue, orders = ordersCount, products = productsCount });
        }

        public IActionResult Logs(string search, string roleFilter = "All")
        {
            if (!IsAdmin()) return Redirect("/Account/Login");

            // 1. Получаем всех пользователей
            var usersQuery = _userManager.Users.AsQueryable();

            // 2. Фильтруем по роли (если выбрана конкретная)
            if (roleFilter != "All")
            {
                usersQuery = usersQuery.Where(u => u.Role == roleFilter);
            }

            // 3. Поиск по логину, почте или ФИО
            if (!string.IsNullOrEmpty(search))
            {
                usersQuery = usersQuery.Where(u =>
                    (u.UserName != null && u.UserName.Contains(search)) ||
                    (u.Email != null && u.Email.Contains(search)) ||
                    (u.FullName != null && u.FullName.Contains(search)));
            }

            var query = _context.ActionLogs.AsQueryable();

            // 4. Если были применены фильтры, оставляем логи только найденных пользователей
            if (!string.IsNullOrEmpty(search) || roleFilter != "All")
            {
                // В логах сохраняется одно из этих значений (FullName ?? Email ?? UserName)
                var matchingNames = usersQuery
                    .Select(u => u.FullName ?? u.Email ?? u.UserName)
                    .Distinct()
                    .ToList();

                if (matchingNames.Any())
                {
                    query = query.Where(l => l.UserName != null && matchingNames.Contains(l.UserName));
                }
                else
                {
                    // Если по фильтрам пользователей не найдено, логи тоже пустые
                    query = query.Where(l => false);
                }
            }

            var logs = query
                .OrderByDescending(l => l.CreatedAt)
                .Take(200)
                .ToList();

            // Передаем параметры обратно во View для сохранения состояния инпутов
            ViewBag.SearchTerm = search;
            ViewBag.RoleFilter = roleFilter;

            return View(logs);
        }

        public IActionResult Users(string search, string roleFilter = "All")
        {
            if (!IsAdmin()) return Redirect("/Account/Login");

            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u =>
                    (u.UserName != null && u.UserName.Contains(search)) ||
                    (u.Email != null && u.Email.Contains(search)) ||
                    (u.FullName != null && u.FullName.Contains(search)));
            }

            if (roleFilter != "All")
            {
                users = users.Where(u => u.Role == roleFilter);
            }

            var model = users.Select(u => new UserViewModel
            {
                Id = u.Id,
                Login = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.PhoneNumber,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            }).ToList();

            ViewBag.SearchTerm = search;
            ViewBag.RoleFilter = roleFilter;

            return View(model);
        }


        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string fullName, string login, string? email, string phone, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                TempData["Error"] = "Пароль должен быть не менее 6 символов";
                return RedirectToAction("Users");
            }

            if (string.IsNullOrWhiteSpace(login))
            {
                TempData["Error"] = "Логин обязателен";
                return RedirectToAction("Users");
            }

            var user = new ApplicationUser
            {
                UserName = login.Trim(),
                Email = !string.IsNullOrWhiteSpace(email) ? email.Trim() : null,
                FullName = fullName,
                PhoneNumber = phone,
                Role = role
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);

                _context.ActionLogs.Add(new ActionLog
                {
                    UserId = 1,
                    UserName = User.Identity?.Name ?? "Admin",
                    Action = "CREATE_USER",
                    Description = $"Создан пользователь {login} с ролью {role}",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Пользователь {login} успешно создан";
            }
            else
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> BulkUpdateRole(List<string> selectedUsers, string role)
        {
            if (!IsAdmin()) return Unauthorized();

            if (selectedUsers == null || selectedUsers.Count == 0)
            {
                TempData["Error"] = "Не выбрано ни одного пользователя";
                return RedirectToAction("Users");
            }

            int count = 0;
            foreach (var id in selectedUsers)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user != null)
                {
                    user.Role = role;
                    await _userManager.UpdateAsync(user);
                    count++;
                }
            }

            TempData["Success"] = $"Роль успешно изменена у {count} пользователей";
            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> BulkDeleteUsers(List<string> selectedUsers)
        {
            if (!IsAdmin()) return Unauthorized();

            if (selectedUsers == null || selectedUsers.Count == 0)
            {
                TempData["Error"] = "Не выбрано ни одного пользователя";
                return RedirectToAction("Users");
            }

            int count = 0;
            var currentUserId = _userManager.GetUserId(User);

            foreach (var id in selectedUsers)
            {
                if (id == currentUserId) continue;

                var user = await _userManager.FindByIdAsync(id);
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                    count++;
                }
            }

            TempData["Success"] = $"Успешно удалено {count} пользователей";
            return RedirectToAction("Users");
        }
    }
}