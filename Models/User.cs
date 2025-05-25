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


        [Required(ErrorMessage = "帳號為必填")]
        [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "帳號只能包含英文字母與數字，且不能有空格")]
        public string Username { get; set; } 

        [Required]
        [Phone]
        public string Phone { get; set; } 

        [Required(ErrorMessage = "Email 為必填")]
        [EmailAddress(ErrorMessage ="Email格式不正確")]
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
        //留言板
        // ✅ 補上與 Review 的一對多關聯
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        //商品總攬
        public SellerApplication SellerApplication { get; set; }





    }
}
