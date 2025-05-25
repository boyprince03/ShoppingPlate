using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class TestController : Controller
{
    // 共用：Admin + Seller 都可以看
    [Authorize(Roles = "Admin,Seller")]
    public IActionResult Shared()
    {
        ViewBag.Role = "Admin 或 Seller 可見";
        return View("TestPage");
    }

    // 僅限 Admin
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        ViewBag.Role = "只有 Admin 可見";
        return View("TestPage");
    }

    // 僅限 Seller
    [Authorize(Roles = "Seller")]
    public IActionResult SellerOnly()
    {
        ViewBag.Role = "只有 Seller 可見";
        return View("TestPage");
    }
}
