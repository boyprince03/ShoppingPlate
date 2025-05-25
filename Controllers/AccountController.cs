using Microsoft.AspNetCore.Mvc;
using ShoppingPlate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;


public class AccountController : Controller
{
    private readonly ShoppingPlate.Data.ApplicationDbContext _context;

    public AccountController(ShoppingPlate.Data.ApplicationDbContext context)
    {
        _context = context;
    }

    // 登入認證方法
    private async Task SignInUser(User user)
    {
        //載入商店名稱
        //var storeName = _context.SellerApplications
        //    .Where(a => a.UserId == user.Id && a.Status == ApplicationStatus.Approved)
        //    .Select(a => a.StoreName)
        //    .FirstOrDefault();
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
        HttpContext.Session.SetString("Phone", user.Phone);
        //HttpContext.Session.SetString("StoreName", storeName);
        HttpContext.Session.SetInt32("LoginRole", (int)user.LoginRole);
        HttpContext.Session.SetString("Role", user.LoginRole.ToString());  // 假設 user.LoginRole 是 enum
        HttpContext.Session.SetString("IsLoggedIn", "true");



    }

    // 註冊頁面
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(User user)
    {
        user.Address = "未填寫";
        if (!ModelState.IsValid)
            return View(user);

        //Email重複驗證
        if (_context.Users.Any(u => u.Email == user.Email))
        {
            ModelState.AddModelError("Email", "Email 已註冊過！");
            return View(user);
        }
        // 帳號重複驗證
        if (_context.Users.Any(u => u.Username == user.Username))
        {
            ModelState.AddModelError("Username", "此帳號已被使用");
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

    // 登入畫面
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string users, string email, string password, string? returnUrl)
    {
        var user = _context.Users.FirstOrDefault(u =>
        (u.Email == email && u.Username == users && u.Password == password));

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

    // 登出
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync("MyCookieAuth");
        return RedirectToAction("Index", "Home");
    }
    //修改帳號資訊
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login");

        var user = await _context.Users
            .Include(u => u.SellerApplication) // ✅ 帶出商店資料
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        return View(user);
    }


    [HttpPost]
    public IActionResult Edit(User updatedUser, string ConfirmPassword, string? StoreName)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login");

        var user = _context.Users
            .Include(u => u.SellerApplication)
            .FirstOrDefault(u => u.Id == userId);

        if (user == null) return NotFound();

        // ✅ 若密碼有輸入才處理
        if (!string.IsNullOrEmpty(updatedUser.Password))
        {
            if (updatedUser.Password != ConfirmPassword)
            {
                ModelState.AddModelError("Password", "密碼與確認密碼不一致");
                return View(updatedUser);
            }

            user.Password = updatedUser.Password;
        }

        // ✅ 更新基本資料
        user.Username = updatedUser.Username;
        user.Email = updatedUser.Email;
        user.Phone = updatedUser.Phone;
        user.Address = updatedUser.Address;

        // ✅ 僅當為 Seller/Admin 且商店名稱有輸入時更新商店名稱
        var role = (UserRole?)HttpContext.Session.GetInt32("LoginRole");
        // ✅ 管理員或賣家可以建立/修改商店名稱
        if ((role == UserRole.Seller || role == UserRole.Admin) && !string.IsNullOrWhiteSpace(StoreName))
        {
            var existingApp = _context.SellerApplications
                .FirstOrDefault(a => a.UserId == user.Id && a.Status == ApplicationStatus.Approved);

            if (existingApp != null)
            {
                // 修改現有商店名稱
                existingApp.StoreName = StoreName;
            }
            else
            {
                // 管理員沒有商店資料 → 新增一筆已核准的
                var newApp = new SellerApplication
                {
                    UserId = user.Id,
                    StoreName = StoreName,
                    Status = ApplicationStatus.Approved,
                    ApplyDate = DateTime.Now,
                    ResponseDate = DateTime.Now
                };
                _context.SellerApplications.Add(newApp);
            }
        }


        _context.SaveChanges();
        TempData["Success"] = "資料更新成功！";
        return RedirectToAction("Settings");
    }


    //修改帳號要再次驗證
    [HttpGet]
    public IActionResult VerifyPassword(string returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public IActionResult VerifyPassword(string password, string returnUrl)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login");

        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null || user.Password != password) // 若有加密要用 Hash 驗證
        {
            ViewBag.Error = "密碼錯誤，請重試。";
            return View();
        }

        // 密碼正確，導向原本想去的頁面
        if (!string.IsNullOrEmpty(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Settings");
    }

    //帳號資訊(setting)
    [HttpGet]
    public IActionResult Settings()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return Redirect($"/Account/Login?returnUrl=/Account/Settings");
        }

        // ✅ Include SellerApplication 才能在 View 中使用
        var user = _context.Users
            .Include(u => u.SellerApplication)
            .FirstOrDefault(u => u.Id == userId);

        if (user == null)
            return NotFound();

        return View(user);
    }


    //直接升級:棄用
    //[HttpPost]
    //public async Task<IActionResult> UpgradeToSellerConfirm()
    //{
    //    int? userId = HttpContext.Session.GetInt32("UserId");
    //    if (userId == null)
    //        return RedirectToAction("Login");

    //    var user = await _context.Users.FindAsync(userId.Value);
    //    if (user == null)
    //        return NotFound();

    //    user.LoginRole = UserRole.Seller;
    //    await _context.SaveChangesAsync();

    //    HttpContext.Session.SetInt32("LoginRole", (int)user.LoginRole);

    //    // 更新 Claims → 重新登入一次
    //    await SignInUser(user);

    //    TempData["Success"] = "成功開啟賣家功能！";
    //    return RedirectToAction("Dashboard", "Seller");
    //}

    //申請成為賣家
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
    //權限不足導向
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View(); // 對應 Views/Account/AccessDenied.cshtml
    }
    //及時檢查Email是否重複-->register.cshtml
    [HttpGet]
    public JsonResult CheckEmailExists(string email)
    {
        bool exists = _context.Users.Any(u => u.Email == email);
        return Json(new { exists });
    }
    //及時檢查帳號是否重複-->register.cshtml
    [HttpGet]
    public JsonResult CheckUsernameExists(string username)
    {
        bool exists = _context.Users.Any(u => u.Username == username);
        return Json(new { exists });
    }
    //檢查Phone


}
