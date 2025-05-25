using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingPlate.Models;
using ShoppingPlate.Data;

public class HomeController : Controller
{
    private readonly ShoppingPlate.Data.ApplicationDbContext _context;

    public HomeController(ShoppingPlate.Data.ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? category)
    {
        var products = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.IsPublished)  // 只顯示上架商品
            .AsQueryable();

        var categoryList = await _context.Categories.ToListAsync();
        ViewBag.Categories = categoryList;

        if (!string.IsNullOrEmpty(category))
        {
            // 只篩選存在的分類
            if (categoryList.Any(c => c.Name == category))
            {
                products = products.Where(p => p.Category.Name == category);
            }
        }

        return View(await products.ToListAsync());
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
