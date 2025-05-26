// Controllers/ProductController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShoppingPlate.Models;
using Microsoft.EntityFrameworkCore;


public class ProductController : Controller
{
    private readonly ShoppingPlate.Data.ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ProductController(ShoppingPlate.Data.ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }
    //搜尋邏輯
    [HttpGet]
    public async Task<IActionResult> Search(string? query, string? category)
    {
        var products = _context.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsPublished);

        if (!string.IsNullOrEmpty(query))
        {
            products = products.Where(p =>
                p.Name.Contains(query) ||
                (p.Category != null && p.Category.Name.Contains(query)));
        }

        if (!string.IsNullOrEmpty(category))
        {
            products = products.Where(p =>
                p.Category != null && p.Category.Name == category);
        }

        ViewBag.Categories = await _context.Categories.ToListAsync();

        return View("Index", await products.ToListAsync());
    }

    // 上下架按鈕功能
    [HttpPost]
    public IActionResult TogglePublish(int id)
    {
        var product = _context.Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            TempData["Error"] = "找不到該商品。";
            return RedirectToAction("SellerDashboard", "Seller");
        }

        product.IsPublished = !product.IsPublished;
        _context.SaveChanges();

        return RedirectToAction("Dashboard", "Seller");

    }



    // 上架頁面 (GET)
    [HttpGet]
    public IActionResult Create(int? selectedCategoryId = null)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        int? role = HttpContext.Session.GetInt32("LoginRole");

        if (userId == null)
            return RedirectToAction("Login", "Account");

        if (role != (int)UserRole.Seller && role != (int)UserRole.Admin)
            return RedirectToAction("AccessDenied", "Account");

        // 檢查是否被停用
        var sellerApp = _context.SellerApplications
            .FirstOrDefault(s => s.UserId == userId && s.Status == ApplicationStatus.Approved);

        if (sellerApp == null || sellerApp.IsDisabled)
        {
            TempData["Error"] = "❌ 您的商店目前已被停用或尚未核准，無法使用商品上架功能。";
            return RedirectToAction("Dashboard", "Seller");
        }

        ViewBag.Categories = _context.Categories.ToList();
        ViewBag.SelectedCategoryId = selectedCategoryId ?? TempData["SelectedCategoryId"];
        ViewBag.HasUploaded = TempData["HasUploaded"] as bool? ?? false;
        ViewBag.SuccessMessage = TempData["SuccessMessage"]?.ToString();

        return View();
    }

    // 上架商品 (POST)
    [HttpPost]
    public async Task<IActionResult> Create(Product product, List<IFormFile> imageFiles)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
            return RedirectToAction("Login", "Account");

        // 檢查是否被停用
        var sellerApp = _context.SellerApplications
            .FirstOrDefault(s => s.UserId == userId && s.Status == ApplicationStatus.Approved);

        if (sellerApp == null || sellerApp.IsDisabled)
        {
            TempData["Error"] = "❌ 您的商店目前已被停用或尚未核准，無法使用商品上架功能。";
            return RedirectToAction("Dashboard", "Seller");
        }

        if (!ModelState.IsValid)
        {
            Console.WriteLine("⚠️ 表單驗證失敗：");

            foreach (var key in ModelState.Keys)
            {
                foreach (var error in ModelState[key].Errors)
                {
                    Console.WriteLine($"欄位 {key}：{error.ErrorMessage}");
                }
            }

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.SelectedCategoryId = product.CategoryId;
            ViewBag.HasUploaded = false;
            return View(product);
        }

        // 處理圖片
        if (imageFiles != null && imageFiles.Count > 0)
        {
            product.Images = new List<ProductImage>();

            foreach (var image in imageFiles)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                var savePath = Path.Combine(_env.WebRootPath, "uploads");

                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                var fullPath = Path.Combine(savePath, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                await image.CopyToAsync(stream);

                product.Images.Add(new ProductImage { Url = "/uploads/" + fileName });
            }
        }

        product.SellerId = userId.Value;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        TempData["HasUploaded"] = true;
        TempData["SelectedCategoryId"] = product.CategoryId;
        TempData["SuccessMessage"] = "✅ 商品上架成功！";

        return RedirectToAction("Create");
    }


    //編輯商品: 
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var product = _context.Products
            .Include(p => p.Images)
            .FirstOrDefault(p => p.Id == id);

        if (product == null) return NotFound();

        // 權限檢查（可擴充）
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin" && role != "Seller")
            return Forbid();

        ViewBag.Categories = _context.Categories.ToList();
        return View(product);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(Product product, List<IFormFile> imageFiles)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin" && role != "Seller")
            return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        var dbProduct = _context.Products
            .Include(p => p.Images)
            .FirstOrDefault(p => p.Id == product.Id);

        if (dbProduct == null) return NotFound();

        // 更新欄位
        dbProduct.Name = product.Name;
        dbProduct.Description = product.Description;
        dbProduct.Price = product.Price;
        dbProduct.Stock = product.Stock;
        dbProduct.CategoryId = product.CategoryId;

        // 圖片處理（簡易：清除舊圖，重上傳）
        if (imageFiles != null && imageFiles.Count > 0)
        {
            dbProduct.Images.Clear();

            var savePath = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

            foreach (var file in imageFiles)
            {
                var ext = Path.GetExtension(file.FileName).ToLower();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowed.Contains(ext)) continue;

                var fileName = Guid.NewGuid() + ext;
                var fullPath = Path.Combine(savePath, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);

                dbProduct.Images.Add(new ProductImage { Url = fileName });
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Details", new { id = product.Id });
    }
    // 刪除商品（只允許管理員與賣家）
    [HttpPost]
    [Authorize(Roles = "Admin,Seller")]
    public IActionResult Delete(int id)
    {
        var product = _context.Products.Find(id);
        if (product == null)
        {
            return NotFound();
        }
        //刪除圖片
        foreach (var img in product.Images)
        {
            var filePath = Path.Combine(_env.WebRootPath, img.Url.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        _context.Products.Remove(product);
        _context.SaveChanges();
        return RedirectToAction("Index", "Home");
    }

    //商品詳細資料列表--> Controllers/ProductController.cs
    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Include(p => p.Reviews)
                .ThenInclude(r => r.User) // 加這行才能取得 review.User.Username
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        // 加入登入者角色，View 判斷用
        ViewBag.Role = HttpContext.Session.GetString("Role");

        return View(product);
    }

    //add 留言方法
    [HttpPost]
    public async Task<IActionResult> Add(Review review)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
            return RedirectToAction("Login", "Account");

        review.UserId = userId.Value;
        review.CreatedAt = DateTime.Now;
        review.Approved = false; // 預設未審核

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Product", new { id = review.ProductId });
    }


    private int GetCurrentUserId()
    {
        return int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }


}
