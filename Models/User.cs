using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoppingPlate.Models
{
    public enum UserRole
    {
        Customer = 0,
        Admin = 1,
        Seller = 2
    }

    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } 

        [Required]
        [Phone]
        public string Phone { get; set; } 

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
        [Required]
        [NotMapped]
        [Compare("Password", ErrorMessage = "兩次輸入的密碼不一致")]
        public string? ConfirmPassword { get; set; }


        public string Address { get; set; } = string.Empty;

        public UserRole LoginRole { get; set; } = UserRole.Customer;


        // 關聯訂單
        public ICollection<Order> Orders { get; set; } = new List<Order>();


    }
}
