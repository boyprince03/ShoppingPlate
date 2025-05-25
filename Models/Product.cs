// Models/Product.cs
using System.ComponentModel.DataAnnotations;
using ShoppingPlate.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;


namespace ShoppingPlate.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "商品名稱為必填")]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public bool IsPublished { get; set; } = true;  // 控制商品是否上架
        [Required(ErrorMessage = "請選擇分類")]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
  
        public Category? Category { get; set; }

        public int SellerId { get; set; }

        [ForeignKey("SellerId")]
        public User? Seller { get; set; }
        public int ViewCount { get; set; } = 0; // 新增瀏覽次數

        // 多張圖片的關聯
        public ICollection<ProductImage>? Images { get; set; }
        //留言板
        public ICollection<Review> Reviews { get; set; } = new List<Review>();


    }
}

