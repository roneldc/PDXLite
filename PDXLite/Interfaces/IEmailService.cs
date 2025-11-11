namespace PDXLite.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string toEmail, string fullName, string confirmationToken);
        Task SendWelcomeEmailAsync(string toEmail, string fullName, string apiKey);
    }
}
