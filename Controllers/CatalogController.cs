using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAtena.Models;
using WebAtena.Services;

namespace WebAtena.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RecommendationService _aiService;

        public CatalogController(
            ApplicationDbContext context,
            RecommendationService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        public async Task<IActionResult> Index(
            string search,
            int? categoryId)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(search) ||
                    (p.ShortDescription != null &&
                     p.ShortDescription.Contains(search)) ||
                    (p.Description != null &&
                     p.Description.Contains(search)));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                productsQuery = productsQuery.Where(
                    p => p.CategoryId == categoryId.Value);
            }

            var products = await productsQuery.ToListAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            int activityScore = 5;
            if (userId != null)
            {
                int orderCount = await _context.Orders.CountAsync(o => o.UserId == userId);
                activityScore = 5 + (orderCount * 10);
            }

            products = await _aiService.RankProducts(userId ?? "anonymous", products, activityScore);

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentCategory = categoryId;
            ViewBag.SearchTerm = search;

            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}