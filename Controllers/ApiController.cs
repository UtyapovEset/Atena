using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAtena.Models;

namespace WebAtena.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
        }

        [HttpGet("products/{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories.ToListAsync();
        }

        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .ToListAsync();
        }

        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            return new
            {
                products = await _context.Products.CountAsync(),
                orders = await _context.Orders.CountAsync(),
                categories = await _context.Categories.CountAsync(),
                totalRevenue = await _context.OrderItems.SumAsync(i => i.Price * i.Quantity)
            };
        }
    }
}