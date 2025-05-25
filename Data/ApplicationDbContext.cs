using Microsoft.EntityFrameworkCore;
using ShoppingPlate.Models;

namespace ShoppingPlate.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        // Data/ShoppingPlateContext.cs
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Payment> Payments { get; set; }
     
        public DbSet<SellerApplication> SellerApplications { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 價格精度設定
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            // ✅ 修正 Review 與 User 關聯
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User) // 評論者
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);


            // ✅ 補上 Review 與 Product 關聯
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)  // 請確認 Product.cs 有 ICollection<Review> Reviews 屬性
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // 根據需求可改成 Restrict

            modelBuilder.Entity<ProductImage>()
                .HasOne(p => p.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }

}
