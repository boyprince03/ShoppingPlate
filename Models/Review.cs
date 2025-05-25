using System;
using System.ComponentModel.DataAnnotations;

namespace ShoppingPlate.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "星等必須介於 1 到 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "留言內容不能為空")]
        [StringLength(500, ErrorMessage = "留言長度不可超過 500 字")]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool Approved { get; set; } = false;
    }
}
