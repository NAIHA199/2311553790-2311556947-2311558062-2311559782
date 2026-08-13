using System.Net;
using System.Net.Mail;
namespace LibraryAdvanced.Services;

public class EmailService : InterfaceEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var smtpSettings = _config.GetSection("SmtpSettings");

            // Loại bỏ khoảng trắng trong Password nếu có
            string password = smtpSettings["Password"]?.Replace(" ", "") ?? "";

            using var client = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"]))
            {
                Credentials = new NetworkCredential(smtpSettings["Username"], password),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"])
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["FromEmail"], smtpSettings["FromName"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
        catch (SmtpException ex)
        {
            // In ra lỗi gốc chi tiết ở cửa sổ Output/Console của Visual Studio
            var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            System.Diagnostics.Debug.WriteLine($"[LỖI GỬI MAIL]: {ex.Message} | Chi tiết: {innerMsg}");

            // Ném lại lỗi chứa thông tin chi tiết hơn
            throw new Exception($"Lỗi gửi mail: {ex.Message} -> Lỗi gốc: {innerMsg}", ex);
        }
    }
}