using ShoppingPlate.Models;

namespace ShoppingPlate.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        context.Database.EnsureCreated();
        //// 🧹 Step 1: 清空 Product (必須先清這張，有外鍵指到 Categories)
        //if (context.Products.Any())
        //{
        //    context.Products.RemoveRange(context.Products);
        //    context.SaveChanges();
        //}

        //// 🧹 Step 2: 清空 Category
        //if (context.Categories.Any())
        //{
        //    context.Categories.RemoveRange(context.Categories);
        //    context.SaveChanges();
        //}
        // 管理員
        var admin = context.Users.FirstOrDefault(u => u.Username == "admin" && u.LoginRole == UserRole.Admin);
        if (admin == null)
        {
            admin = new User
            {
                Username = "admin",
                Phone = "0929511011",
                Email = "teamone004@gmail.com",
                Password = "123456",
                LoginRole = UserRole.Admin,
                Address = "台北市信義區"
            };
            context.Users.Add(admin);
        }

        var seller = context.Users.FirstOrDefault(u => u.Username == "demoSeller" && u.LoginRole == UserRole.Seller);
        if (seller == null)
        {
            seller = new User
            {
                Username = "demoSeller",
                Phone = "0929xxxxxx",
                Email = "demoseller@example.com",
                Password = "123456",
                LoginRole = UserRole.Seller,
                Address = "台北市信義區"
            };
            context.Users.Add(seller);
        }

        var customer = context.Users.FirstOrDefault(u => u.Username == "demoCustomer" && u.LoginRole == UserRole.Customer);
        if (customer == null)
        {
            customer = new User
            {
                Username = "demoCustomer",
                Phone = "0929xxxxxx",
                Email = "democustomer@example.com",
                Password = "123456",
                LoginRole = UserRole.Customer,
                Address = "台北市信義區"
            };
            context.Users.Add(customer);
        }

        context.SaveChanges();


        // 分類（僅補缺）
        string[] requiredCategories = { "家電類", "手機", "家俱", "服飾" };
        var existingCategoryNames = context.Categories.Select(c => c.Name).ToList();
        var newCategories = requiredCategories
            .Where(name => !existingCategoryNames.Contains(name))
            .Select(name => new Category { Name = name })
            .ToList();
        if (newCategories.Any())
        {
            context.Categories.AddRange(newCategories);
            context.SaveChanges();
        }

        var categoryDict = context.Categories.ToDictionary(c => c.Name, c => c.Id);

        // ✅ 預設圖片 URL base 路徑
        string baseImagePath = "/uploads/";

        // 預設商品 + 圖片
        if (!context.Products.Any())
        {
            var products = new Product[]
            {
            new Product
            {
                Name = "白色T恤",
                Description = "100% 純棉白T恤",
                Price = 299,
                Stock = 50,
                CategoryId = categoryDict["服飾"],
                SellerId = admin.Id,
                Images = new List<ProductImage>
                {
                    new ProductImage { Url = baseImagePath + "default-shirt.png" }
                }
            },
            new Product
            {
                Name = "iPhone 14",
                Description = "Apple 最新款",
                Price = 29900,
                Stock = 20,
                CategoryId = categoryDict["手機"],
                SellerId = admin.Id,
                Images = new List<ProductImage>
                {
                    new ProductImage { Url = baseImagePath + "default-iphone.png" }
                }
            },
            new Product
            {
                Name = "Dyson 吸塵器",
                Description = "強力吸塵器",
                Price = 12900,
                Stock = 15,
                CategoryId = categoryDict["家電類"],
                SellerId = admin.Id,
                Images = new List<ProductImage>
                {
                    new ProductImage { Url = baseImagePath + "default-dyson.png" }
                }
            }
            };

            context.Products.AddRange(products);
            context.SaveChanges();
        }
    }

}
