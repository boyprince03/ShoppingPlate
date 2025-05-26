using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingPlate.Models;
using Microsoft.AspNetCore.Authorization;
using ShoppingPlate.Services;

//[Authorize(Roles = "Admin")]

public class AdminController : Controller
{
    private readonly ShoppingPlate.Data.ApplicationDbContext _context;
    private readonly EmailService _emailService;

    public AdminController(ShoppingPlate.Data.ApplicationDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }



    public async Task<IActionResult> Dashboard()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var user = await _context.Users.FindAsync(userId.Value);
        var role = (UserRole?)HttpContext.Session.GetInt32("LoginRole");
        if (role != UserRole.Admin)
            return Unauthorized();

        ViewBag.AdminName = user.Username;
        ViewBag.TotalSales = await _context.Orders.SumAsync(o => o.TotalAmount);
        ViewBag.OrderCount = await _context.Orders.CountAsync();
        ViewBag.AvgOrder = (ViewBag.OrderCount > 0) ? ViewBag.TotalSales / ViewBag.OrderCount : 0;

        ViewBag.TopProducts = await _context.OrderDetails
            .Include(od => od.Product)
            .GroupBy(od => od.ProductId)
            .Select(g => new {
                Product = g.First().Product,
                Quantity = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(5)
            .ToListAsync();

        ViewBag.PopularProducts = await _context.Products
            .OrderByDescending(p => p.ViewCount)
            .Take(5)
            .ToListAsync();

        ViewBag.VisitorCount = TempData["VisitorCount"] ?? 1234;

        // ✅ 顯示待處理申請數
        ViewBag.PendingSellerApplications = await _context.SellerApplications
            .CountAsync(sa => sa.Status == ApplicationStatus.Pending);

        return View();
    }
    //審核seller
    public async Task<IActionResult> SellerApplications()
    {
        var apps = await _context.SellerApplications
            .Include(a => a.User)
            .OrderByDescending(a => a.ApplyDate)
            .ToListAsync();

        return View(apps);
    }
    [Authorize(Roles = "Admin")]

    [HttpPost]
    public async Task<IActionResult> ApproveSeller(int id, bool approve)
    {
        var app = await _context.SellerApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app == null) return NotFound();

        app.Status = approve ? ApplicationStatus.Approved : ApplicationStatus.Rejected;
        app.ResponseDate = DateTime.Now;

        if (approve)
        {
            app.User.LoginRole = UserRole.Seller;
            _emailService.SendSellerApplicationResult(app.User.Email, app.User.Username, true);
        }
        else
        {
            _emailService.SendSellerApplicationResult(app.User.Email, app.User.Username, false);
        }


        await _context.SaveChangesAsync();
        return RedirectToAction("SellerApplications");
    }
    //商品總攬
    public async Task<IActionResult> AllProducts(string keyword, string category)
    {
        var query = _context.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .ThenInclude(s => s.SellerApplication)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(p => p.Name.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category.Name == category);
        }

        var categories = await _context.Categories.Select(c => c.Name).ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.Keyword = keyword;
        ViewBag.SelectedCategory = category;

        return View(await query.ToListAsync());
    }
    // 店鋪管理頁面
    public IActionResult StoreManagement(string? search)
    {
        var query = _context.SellerApplications
            .Where(a => a.Status == ApplicationStatus.Approved)
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a =>
                a.StoreName.Contains(search) ||
                a.User.NameUser.Contains(search));
        }

        return View(query.ToList());
    }
    [HttpPost]
    public IActionResult EnableStore(int id)
    {
        var store = _context.SellerApplications.Find(id);
        if (store != null && store.IsDisabled)
        {
            store.IsDisabled = false;
            _context.SaveChanges();
        }

        return RedirectToAction("StoreManagement");
    }


    [HttpPost]
    public IActionResult DisableStore(int id)
    {
        var store = _context.SellerApplications.Find(id);
        if (store != null && !store.IsDisabled)
        {
            store.IsDisabled = true;
            _context.SaveChanges();
        }

        return RedirectToAction("StoreManagement");
    }


    public IActionResult StoreReport(int id)
    {
        var store = _context.SellerApplications
            .Include(s => s.User)
            .FirstOrDefault(s => s.Id == id);

        if (store == null) return NotFound();

        var sellerId = store.User.Id;

        // 該商店所有商品
        var products = _context.Products
            .Where(p => p.SellerId == sellerId)
            .ToList();

        var productIds = products.Select(p => p.Id).ToList();

        // 所有訂單明細與統計
        var orderDetails = _context.OrderDetails
            .Include(od => od.Product)
            .Where(od => productIds.Contains(od.ProductId))
            .ToList();

        var totalSales = orderDetails.Sum(od => od.Quantity * od.UnitPrice);
        var orderCount = orderDetails
            .Select(od => od.OrderId)
            .Distinct()
            .Count();

        var topProduct = orderDetails
            .GroupBy(od => od.Product.Name)
            .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Quantity) })
            .OrderByDescending(g => g.Total)
            .FirstOrDefault();

        ViewBag.StoreName = store.StoreName;
        ViewBag.SellerName = store.User.NameUser;
        ViewBag.TotalSales = totalSales;
        ViewBag.OrderCount = orderCount;
        ViewBag.ProductCount = products.Count;
        ViewBag.TopProduct = topProduct != null ? $"{topProduct.Name}（{topProduct.Total} 件）" : "無資料";

        return View();
    }



    // 帳號管理頁面（預留）
    public IActionResult UserManagement()
    {
        // TODO: 未來可串黑名單 / VIP 功能
        return View(); // 建立 Views/Admin/UserManagement.cshtml
    }



}
