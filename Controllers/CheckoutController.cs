// Controllers/CheckoutController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingPlate.Data;
using ShoppingPlate.Models;
using ShoppingPlate.Services;

public class CheckoutController : Controller
{
    private readonly ShoppingPlate.Data.ApplicationDbContext _context;
    private readonly EmailService _emailService;

    public CheckoutController(ShoppingPlate.Data.ApplicationDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    //結帳方法
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = await GetCartItems();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId != null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                ViewData["Name"] = user.Username;
                ViewData["Phone"] = user.Phone;
                ViewData["Email"] = user.Email;
                ViewData["Address"] = user.Address;
            }
        }

        return View(cart);
    }





    [HttpPost]
    public async Task<IActionResult> Index(string name, string phone, string email, string address, bool registerAsMember, string? password)
    {
        var cartItems = await GetCartItems();
        if (!cartItems.Any())
            return RedirectToAction("Index", "Cart");

        // 如果選擇註冊會員
        if (registerAsMember)
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == email);
            if (!exists)
            {
                var user = new User
                {
                    Username = name,
                    Phone = phone,
                    Email = email,
                    Password = password, // 建議實際專案中使用雜湊加密
                    Address=address,
                    LoginRole = 0
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                HttpContext.Session.SetString("UserEmail", email);
            }
        }

        var sellerEmail = cartItems.FirstOrDefault()?.Product?.Seller?.Email;

        // 如果查不到 sellerEmail，可以根據情境選擇是否中斷或記錄
        if (string.IsNullOrEmpty(sellerEmail))
        {
            // 你可以選擇跳回錯誤畫面，或預設為 null
            // return BadRequest("找不到賣家 Email");
            sellerEmail = null; // 或者寫 log
        }

        var order = new Order
        {
            OrderDate = DateTime.Now,
            Status = "處理中",
            TotalAmount = cartItems.Sum(i => i.Product.Price * i.Quantity),
            CustomerName = name,
            CustomerPhone = phone,
            CustomerEmail = email,
            SellerEmail = sellerEmail,  // ⬅️ 這裡自動填入第一筆商品賣家的信箱
            ShippingAddress = address,
            OrderDetails = cartItems.Select(c => new OrderDetail
            {
                ProductId = c.ProductId,
                Quantity = c.Quantity,
                UnitPrice = c.Product.Price
            }).ToList()
        };

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cartItems);

        await _context.SaveChangesAsync();
        _emailService.SendOrderConfirmation(order);

        return RedirectToAction("Success", new { id = order.Id });
    }
    //確認訂單，導向success頁面
    public async Task<IActionResult> Success(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        return View(order);
    }

    //查詢方法
    private string GetSessionId()
    {
        if (!HttpContext.Session.TryGetValue("SessionId", out var value))
        {
            value = Guid.NewGuid().ToByteArray();
            HttpContext.Session.Set("SessionId", value);
        }
        return BitConverter.ToString(value).Replace("-", "");
    }

    //列出查詢
    private async Task<List<CartItem>> GetCartItems()
    {
        var sessionId = GetSessionId();
        return await _context.CartItems
            .Include(c => c.Product)
            .ThenInclude(p => p.Seller)   // ⬅️ 要有這一行！
            .Where(c => c.SessionId == sessionId)
            .ToListAsync();
    }

    [HttpGet]
    public IActionResult Query()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Query(string name, string phone)
    {
        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(d => d.Product)
            .Where(o => o.CustomerName == name && o.CustomerPhone == phone)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View("QueryResult", orders);
    }
    //取消
    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null || order.Status != "處理中")
        {
            return NotFound();
        }

        order.Status = "已取消";
        await _context.SaveChangesAsync();
        _emailService.SendOrderCancellation(order, "Customer");

        TempData["CancelMessage"] = $"訂單 #{id} 已成功取消。";
        return RedirectToAction("Query");
    }

}
