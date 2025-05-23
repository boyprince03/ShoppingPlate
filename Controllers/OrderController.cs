using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingPlate.Data;
using ShoppingPlate.Services;
using ShoppingPlate.Models;
using ShoppingPlate.Services;

namespace ShoppingPlate.Controllers
{
    public class OrderController : Controller
    {
        private readonly ShoppingPlateContext _context;
        public OrderController(ShoppingPlateContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> QuickLookup()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/Account/Login?returnUrl=/Order/QuickLookup");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("找不到使用者");
            }

            // 查詢訂單 (比對 CustomerName 和 Phone)
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.CustomerName == user.Username && o.CustomerPhone == user.Phone)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            ViewData["Source"] = "QuickLookup";

            return View("~/Views/Checkout/QueryResult.cshtml", orders);

        }

    }
}
