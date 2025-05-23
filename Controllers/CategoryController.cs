using Microsoft.AspNetCore.Mvc;
using ShoppingPlate.Data;
using ShoppingPlate.Models;

public class CategoryController : Controller
{
    private readonly ShoppingPlateContext _context;

    public CategoryController(ShoppingPlateContext context)
    {
        _context = context;
    }

    // ✅ 新增分類（避免重複 + 錯誤處理）
    [HttpPost]
    public IActionResult Add([FromBody] Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
            return BadRequest("分類名稱不得為空");

        var exists = _context.Categories.Any(c => c.Name == category.Name);
        if (exists)
            return Conflict("分類名稱已存在");

        _context.Categories.Add(category);
        _context.SaveChanges();

        return Json(new { id = category.Id, name = category.Name });
    }

    // ✅ 刪除分類（有綁商品的情況不允許刪）
    [HttpPost]
    public IActionResult Delete(int id)
    {
        var cat = _context.Categories.Find(id);
        if (cat == null)
            return NotFound();

        var hasProduct = _context.Products.Any(p => p.CategoryId == id);
        if (hasProduct)
            return BadRequest("此分類已有商品，無法刪除");

        _context.Categories.Remove(cat);
        _context.SaveChanges();

        return Ok();
    }

    // ✅ 編輯分類
    [HttpPost]
    public IActionResult Edit(int id, [FromBody] Category updated)
    {
        if (string.IsNullOrWhiteSpace(updated.Name))
            return BadRequest("名稱不得為空");

        var cat = _context.Categories.Find(id);
        if (cat == null)
            return NotFound();

        cat.Name = updated.Name;
        _context.SaveChanges();

        return Ok();
    }
}
