using Microsoft.AspNetCore.Mvc;
using ShoppingPlate.Data;
using ShoppingPlate.Models;
using Microsoft.EntityFrameworkCore;

public class ReviewController : Controller
{
    private readonly ShoppingPlate.Data.ApplicationDbContext _context;

    public ReviewController(ShoppingPlate.Data.ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Add(Review review)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        review.UserId = userId.Value;
        review.CreatedAt = DateTime.Now;
        review.Approved = false;

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Product", new { id = review.ProductId });
    }
}
