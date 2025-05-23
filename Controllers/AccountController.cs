// Controllers/AccountController.cs
using Microsoft.AspNetCore.Mvc;
using ShoppingPlate.Models;
using ShoppingPlate.Data;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;


public class AccountController : Controller
{
    private readonly ShoppingPlateContext _context;

    public AccountController(ShoppingPlateContext context)
    {
        _context = context;
    }

    // 註冊頁面
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public IActionResult Register(User user)
    {
        user.Address = "你的住址";
        Console.WriteLine($"💡 Address = {user.Address}");

        if (!ModelState.IsValid)
        {
            Console.WriteLine("🔍 ModelState 無效！");
            foreach (var error in ModelState)
            {
                Console.WriteLine($"欄位：{error.Key}");
                foreach (var err in error.Value.Errors)
                {
                    Console.WriteLine($"❌ 錯誤：{err.ErrorMessage}");
                }
            }
            return View(user); // ⚠️ 錯誤的話不應繼續執行後續
        }

        var exists = _context.Users.Any(u => u.Email == user.Email);
        if (exists)
        {
            ModelState.AddModelError("Email", "Email 已註冊過！");
            return View(user);
        }

        user.LoginRole = UserRole.Customer;

        try
        {
            _context.Users.Add(user);
            Console.WriteLine($"✅ Password = {user.Password}");
            Console.WriteLine($"✅ ConfirmPassword = {user.ConfirmPassword}");

            _context.SaveChanges();
            Console.WriteLine("✅ 新增成功，ID：" + user.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ 儲存失敗：" + ex.Message);
            return View(user); // ⚠️ 儲存失敗也該 return View(user)
        }

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetInt32("LoginRole", (int)user.LoginRole);

        switch (user.LoginRole)
        {
            case UserRole.Admin:
                return RedirectToAction("Dashboard", "Admin");
            case UserRole.Seller:
                return RedirectToAction("Dashboard", "Seller");
            default:
                return RedirectToAction("Index", "Home");
        }
    }


    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }


    [HttpPost]
    public IActionResult Login(string email, string password, string? returnUrl)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

        if (user == null)
        {
            ViewBag.Error = "帳號或密碼錯誤";
            return View();
        }

        // 登入成功 → 設定 Session
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetInt32("LoginRole", (int)user.LoginRole);
        HttpContext.Session.SetString("IsLoggedIn", "true");

        // 若有 returnUrl（例如設定頁進來的），優先導回原頁
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // ✅ 沒 returnUrl 才導向對應角色頁面
        return user.LoginRole switch
        {
            UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
            UserRole.Seller => RedirectToAction("Dashboard", "Seller"),
            _ => RedirectToAction("Index", "Home")
        };
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

        HttpContext.Session.SetInt32("LoginRole", (int)user.LoginRole); // 重寫 Session

        TempData["Success"] = "成功開啟賣家功能！";
        return RedirectToAction("Dashboard", "Seller");
    }

    //Logout
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
    //edit User info
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

        // 更新欄位
        user.Username = updatedUser.Username;
        user.Phone = updatedUser.Phone;
        user.Email = updatedUser.Email;
        user.Address = updatedUser.Address;

        if (!string.IsNullOrEmpty(updatedUser.Password))
        {
            user.Password = updatedUser.Password; // ⚠️ 實務上應加密
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
            // 用 Session 判斷是否登入
            return Redirect($"/Account/Login?returnUrl=/Account/Settings");
        }

        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null) return NotFound();

        return View(user);
    }

    //seller審核申請
    [HttpGet]
    public IActionResult ApplySeller()
    {
        return View();
    }

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

    ////Upgrade to Seller
    //[HttpGet]
    //public async Task<IActionResult> UpgradeToSeller()
    //{
    //    int? userId = HttpContext.Session.GetInt32("UserId");
    //    if (userId == null)
    //        return RedirectToAction("Login");

    //    var user = await _context.Users.FindAsync(userId.Value);
    //    if (user == null || user.LoginRole == UserRole.Admin)
    //        return Unauthorized();

    //    return View(user);
    //}
}
