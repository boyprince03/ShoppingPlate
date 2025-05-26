using Microsoft.AspNetCore.Http;
using ShoppingPlate.Data;
using ShoppingPlate.Models;
using System.Linq;

namespace ShoppingPlate.Helpers
{
    public static class SellerGuard
    {
        //禁用功能共用方法
        public static bool IsSellerDisabled(HttpContext context, ApplicationDbContext db, out string errorMessage)
        {
            errorMessage = string.Empty;

            var userId = context.Session.GetInt32("UserId");
            if (userId == null)
            {
                errorMessage = "尚未登入。";
                return true;
            }

            var sellerApp = db.SellerApplications
                .FirstOrDefault(s => s.UserId == userId && s.Status == ApplicationStatus.Approved);

            if (sellerApp == null)
            {
                errorMessage = "您的商店尚未核准。";
                return true;
            }

            if (sellerApp.IsDisabled)
            {
                errorMessage = "您的商店已被停用，無法使用此功能。";
                return true;
            }

            return false;
        }
    }
}
