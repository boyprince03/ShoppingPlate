using Microsoft.EntityFrameworkCore;
using ShoppingPlate.Data;
using ShoppingPlate.Services;

var builder = WebApplication.CreateBuilder(args);

// 判斷是否為 Production 環境
var isProduction = builder.Environment.IsProduction();

// ✅ 資料庫設定（部署用 SQLite、開發用 SQL Server）
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (isProduction)
    {
        options.UseSqlite("Data Source=shoppingplate.db");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// ✅ Session 設定
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddTransient<EmailService>();

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization();

// ✅ Railway 用 PORT 環境變數綁定 Port
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

var app = builder.Build();

// ✅ 自動初始化資料庫（開發 SQLite 時也會執行）
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbInitializer.Initialize(db);
}

// ✅ 中介軟體設定
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ✅ 預設路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
