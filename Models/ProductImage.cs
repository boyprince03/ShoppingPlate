using System.ComponentModel.DataAnnotations;

namespace ShoppingPlate.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public string Url { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }

}
