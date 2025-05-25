using Microsoft.AspNetCore.Mvc;
using ShoppingPlate.Models;
using ShoppingPlate.Data;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

public class AccountController : Controller
{
    private readonly ShoppingPlateContext _context;

    public AccountController(ShoppingPlateContext context)
    {
        _context = context;
    }

    // ✅ 登入認證方法
    private async Task SignInUser(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.LoginRole.ToString()),
            new Claim("UserId", user.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "MyCookieAuth");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("MyCookieAuth", principal);

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetInt32("LoginRole", (int)user.LoginRole);
    }

    // ✅ 註冊頁面
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(User user)
    {
        user.Address = "你的住址";

        if (!ModelState.IsValid)
            return View(user);

        if (_context.Users.Any(u => u.Email == user.Email))
        {
            ModelState.AddModelError("Email", "Email 已註冊過！");
            return View(user);
        }

        user.LoginRole = UserRole.Customer;

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ 儲存失敗：" + ex.Message);
            return View(user);
        }

        await SignInUser(user);

        return RedirectToAction("Index", "Home");
    }

    // ✅ 登入畫面
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

        if (user == null)
        {
            ViewBag.Error = "帳號或密碼錯誤";
            return View();
        }

        await SignInUser(user);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return user.LoginRole switch
        {
            UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
            UserRole.Seller => RedirectToAction("Dashboard", "Seller"),
            _ => RedirectToAction("Index", "Home")
        };
    }

    // ✅ 登出
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync("MyCookieAuth");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login");

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(User updatedUser)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null || userId != updatedUser.Id) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.Username = updatedUser.Username;
        user.Phone = updatedUser.Phone;
        user.Email = updatedUser.Email;
        user.Address = updatedUser.Address;

        if (!string.IsNullOrEmpty(updatedUser.Password))
        {
            user.Password = updatedUser.Password;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "資料已更新！";
        return RedirectToAction("Edit");
    }

    [HttpGet]
    public IActionResult Settings()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return Redirect($"/Account/Login?returnUrl=/Account/Settings");
        }

        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null) return NotFound();

        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> UpgradeToSellerConfirm()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToAction("Login");

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
            return NotFound();

        user.LoginRole = UserRole.Seller;
        await _context.SaveChangesAsync();

        HttpContext.Session.SetInt32("LoginRole", (int)user.LoginRole);

        // ✅ 更新 Claims → 重新登入一次
        await SignInUser(user);

        TempData["Success"] = "成功開啟賣家功能！";
        return RedirectToAction("Dashboard", "Seller");
    }

    [HttpGet]
    public IActionResult ApplySeller() => View();

    [HttpPost]
    public async Task<IActionResult> ApplySeller(string storeName)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login");

        var exists = await _context.SellerApplications.AnyAsync(a => a.UserId == userId && a.Status == ApplicationStatus.Pending);

        if (exists)
        {
            TempData["Error"] = "您已有待審核的申請";
            return RedirectToAction("Index", "Home");
        }

        var application = new SellerApplication
        {
            UserId = userId.Value,
            StoreName = storeName
        };

        _context.SellerApplications.Add(application);
        await _context.SaveChangesAsync();

        TempData["Success"] = "申請已提交，請等待審核";
        return RedirectToAction("Index", "Home");
    }
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View(); // 對應 Views/Account/AccessDenied.cshtml
    }

}
