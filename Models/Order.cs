using System.ComponentModel.DataAnnotations;

namespace ShoppingPlate.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public DateTime OrderDate { get; set; }

        public string Status { get; set; } = "處理中"; // 處理中、已出貨、已完成、已取消

        public decimal TotalAmount { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string? SellerEmail { get; set; }
        public string ShippingAddress { get; set; }


        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    }
}
