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



}
