// Controllers/SellerController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ShoppingPlate.Models;
using ShoppingPlate.Services;


public class SellerController : Controller
{
    private readonly ShoppingPlate.Data.ApplicationDbContext _context;
    private readonly EmailService _emailService;


    public SellerController(ShoppingPlate.Data.ApplicationDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // 登入後的賣家主控台

    public async Task<IActionResult> Dashboard()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
            return RedirectToAction("Login", "Account");

        var role = (UserRole?)HttpContext.Session.GetInt32("LoginRole");
        if (role != UserRole.Seller && role != UserRole.Admin)
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
            return Unauthorized();

        // 🔽 取得該賣家通過審核的商店名稱
        var storeName = await _context.SellerApplications
            .Where(a => a.UserId == userId && a.Status == ApplicationStatus.Approved)
            .Select(a => a.StoreName)
            .FirstOrDefaultAsync();

        ViewBag.SellerName = storeName ?? user.Username; // 若找不到商店名稱，就顯示帳號名稱

        var myProducts = await _context.Products
            .Where(p => p.SellerId == userId.Value)
            .Include(p => p.Images)
            .ToListAsync();

        return View(myProducts); // 傳送 Model 給前端 View
    }





    // 賣家查看自己的商品所收到的所有訂單
    public async Task<IActionResult> Orders()
    {
        int? sellerId = HttpContext.Session.GetInt32("UserId");

        if (sellerId == null)
        {
            return Unauthorized(); // 或 return RedirectToAction("Login", "Account");
        }
        var role = (UserRole?)HttpContext.Session.GetInt32("LoginRole");

        if (role != UserRole.Seller && role != UserRole.Admin)
        {
            return Unauthorized();
        }

        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Where(o => o.OrderDetails.Any(od => od.Product.SellerId == sellerId.Value))
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }
    //更新數量方法
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int orderId, string status)
    {
        int? sellerId = HttpContext.Session.GetInt32("UserId");
        if (sellerId == null)
            return Unauthorized();
        var role = (UserRole?)HttpContext.Session.GetInt32("LoginRole");

        if (role != UserRole.Seller && role != UserRole.Admin)
        {
            return Unauthorized();
        }

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.OrderDetails.Any(od => od.Product.SellerId == sellerId.Value));

        if (order == null)
            return NotFound();

        order.Status = status;
        await _context.SaveChangesAsync();
        _emailService.SendOrderCancellation(order, "Seller");



        return RedirectToAction("Orders");

    }
    //取消訂單按鈕方法
    [HttpPost]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        int? sellerId = HttpContext.Session.GetInt32("UserId");
        if (sellerId == null)
            return Unauthorized();
        var role = (UserRole?)HttpContext.Session.GetInt32("LoginRole");

        if (role != UserRole.Seller && role != UserRole.Admin)
        {
            return Unauthorized();
        }



        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.OrderDetails.Any(od => od.Product.SellerId == sellerId.Value));

        if (order == null)
            return NotFound();

        // 僅允許處理中狀態下取消
        if (order.Status != "處理中")
        {
            TempData["Error"] = "只能取消處理中的訂單。";
            return RedirectToAction("Orders");
        }

        order.Status = "已取消";
        await _context.SaveChangesAsync();

        TempData["Success"] = $"訂單 #{order.Id} 已取消";
        return RedirectToAction("Orders");
    }


}
