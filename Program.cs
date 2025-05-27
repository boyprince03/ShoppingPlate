// Program.cs
using Microsoft.EntityFrameworkCore;
using ShoppingPlate.Data; // DbContext 的命名空間
using ShoppingPlate.Services;



var builder = WebApplication.CreateBuilder(args);


// 註冊資料庫服務
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session
builder.Services.AddDistributedMemoryCache(); // 分散式快取
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 逾時時間
    options.Cookie.HttpOnly = true;                  // 防止 JS 存取
    options.Cookie.IsEssential = true;               // GDPR 必需 Cookie
});

builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddTransient<EmailService>();

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";           // 未登入時自動導向這裡
        options.AccessDeniedPath = "/Account/AccessDenied"; // 權限不足時導向

    });

builder.Services.AddAuthorization();
builder.Services.AddSession(); // 用 Session 儲存 UserId



var app = builder.Build();
// 啟用 Session


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShoppingPlate.Data.ApplicationDbContext>();
    DbInitializer.Initialize(db); // 執行初始化資料
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// 中介軟體
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();  
app.UseAuthorization();   

app.UseHttpsRedirection();
app.UseStaticFiles(); // 靜態資源的方法(圖片)





// 預設路徑
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
