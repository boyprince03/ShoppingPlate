using System.Net;
using System.Net.Mail;
using System.Text;
using ShoppingPlate.Models;

namespace ShoppingPlate.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    private SmtpClient CreateSmtpClient()
    {
        return new SmtpClient(_config["Smtp:Host"])
        {
            Port = int.Parse(_config["Smtp:Port"]),
            Credentials = new NetworkCredential(
                _config["Smtp:Username"],
                _config["Smtp:Password"]
            ),
            EnableSsl = true,
        };
    }
    //訂單確認通知
    public void SendOrderConfirmation(Order order)
    {
        var smtpClient = CreateSmtpClient();

        var subject = $"🧾 訂單確認 #{order.Id}";
        var body = new StringBuilder();
        body.AppendLine($"感謝您的訂購，{order.CustomerName}！");
        body.AppendLine($"訂單編號：{order.Id}");
        body.AppendLine($"下單時間：{order.OrderDate}");
        body.AppendLine($"總金額：${order.TotalAmount}");
        body.AppendLine($"配送地址：{order.ShippingAddress}");
        body.AppendLine();
        body.AppendLine("商品明細：");

        foreach (var item in order.OrderDetails)
        {
            body.AppendLine($"- {item.Product.Name} x {item.Quantity} @ ${item.UnitPrice}");
        }

        var mail = new MailMessage
        {
            From = new MailAddress(_config["Smtp:From"], "ShoppingPlate 購物平台"),
            Subject = subject,
            Body = body.ToString(),
            IsBodyHtml = false
        };

        mail.To.Add(order.CustomerEmail);

        smtpClient.Send(mail);
    }
    //賣家取消訂單通知買家|| 買家取消訂單通知賣家
    public void SendOrderCancellation(Order order, string cancelledBy)
    {
        var smtpClient = CreateSmtpClient();

        var subject = $"❌ 訂單已取消通知 #{order.Id}";
        var body = new StringBuilder();

        if (cancelledBy == "Seller")
        {
            body.AppendLine($"您好，{order.CustomerName}：");
            body.AppendLine($"很抱歉通知您，您的訂單（編號：{order.Id}）已由賣家於 {DateTime.Now:yyyy-MM-dd HH:mm} 取消。");
            body.AppendLine($"若有任何疑問，請聯絡客服或賣家。");

            // 寄給買家
            var mail = new MailMessage
            {
                From = new MailAddress(_config["Smtp:From"], "ShoppingPlate 購物平台"),
                Subject = subject,
                Body = body.ToString(),
                IsBodyHtml = false
            };
            mail.To.Add(order.CustomerEmail);
            smtpClient.Send(mail);
        }
        else if (cancelledBy == "Customer")
        {
            body.AppendLine($"您好，賣家：");
            body.AppendLine($"訂單（編號：{order.Id}）已由買家 {order.CustomerName} 於 {DateTime.Now:yyyy-MM-dd HH:mm} 取消。");
            body.AppendLine($"請登入後台確認。");

            // 寄給賣家
            var mail = new MailMessage
            {
                From = new MailAddress(_config["Smtp:From"], "ShoppingPlate 購物平台"),
                Subject = subject,
                Body = body.ToString(),
                IsBodyHtml = false
            };
            mail.To.Add(order.SellerEmail);
            smtpClient.Send(mail);
        }
    }


}
