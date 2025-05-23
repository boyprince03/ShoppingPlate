
//預留沒實作
using System.ComponentModel.DataAnnotations;

namespace ShoppingPlate.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public string PaymentMethod { get; set; } // Credit, LINE Pay, etc.
        public string PaymentStatus { get; set; } // Paid, Unpaid, Failed
        public DateTime PaidAt { get; set; }
    }
}
