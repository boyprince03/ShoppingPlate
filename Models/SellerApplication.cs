using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoppingPlate.Models
{
    public enum ApplicationStatus { Pending, Approved, Rejected }

    public class SellerApplication
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public string StoreName { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        public DateTime ApplyDate { get; set; } = DateTime.Now;
        public DateTime? ResponseDate { get; set; }
    }
}
