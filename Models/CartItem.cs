using System.ComponentModel.DataAnnotations;

namespace ShoppingPlate.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? SessionId { get; set; }

        public int? UserId { get; set; }

        public User? User { get; set; }
        [Required]

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }
    }
}
