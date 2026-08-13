namespace LibraryAdvanced.Services
{
    public interface InterfaceEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
