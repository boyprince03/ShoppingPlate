// Controllers/ProductController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShoppingPlate.Models;
using ShoppingPlate.Data;
using Microsoft.EntityFrameworkCore;


public class ProductController : Controller
{
    private readonly ShoppingPlateContext _context;
    private readonly IWebHostEnvironment _env;

    public ProductController(ShoppingPlateContext context, IWebHostEnvironment env)
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






    //上架邏輯
    [HttpGet]
    public IActionResult Create(int? selectedCategoryId = null)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        int? role = HttpContext.Session.GetInt32("LoginRole");

        if (userId == null)
            return RedirectToAction("Login", "Account");

        if (role != (int)UserRole.Seller && role != (int)UserRole.Admin)
            return RedirectToAction("AccessDenied", "Account");

        ViewBag.Categories = _context.Categories.ToList();
        ViewBag.SelectedCategoryId = selectedCategoryId;
        ViewBag.Categories = _context.Categories.ToList();
        ViewBag.HasUploaded = TempData["HasUploaded"] as bool? ?? false;
        ViewBag.SelectedCategoryId = TempData["SelectedCategoryId"] ?? null;
        ViewBag.SuccessMessage = TempData["SuccessMessage"]?.ToString();

        return View();
    }


    [HttpPost]
    public async Task<IActionResult> Create(Product product, List<IFormFile> imageFiles)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

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
            ViewBag.HasUploaded = false; // 顯示「上架商品」
            return View(product);
        }

        //  圖片處理
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

        //  導回原頁面並顯示「繼續上架」
        TempData["HasUploaded"] = true;
        TempData["SelectedCategoryId"] = product.CategoryId;
        TempData["SuccessMessage"] = "✅ 商品上架成功！";
        return RedirectToAction("Create");
    }

//編輯商品: TODO
//商品詳細資料列表: TODO
    private int GetCurrentUserId()
    {
        return int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }


}
