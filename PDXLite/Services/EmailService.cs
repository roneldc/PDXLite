using PDXLite.Interfaces;
using System.Net.Mail;
using System.Net;

namespace PDXLite.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _appUrl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@pdxlite.com";
            _fromName = _configuration["Email:FromName"] ?? "PDXLite";
            _appUrl = _configuration["Email:AppUrl"] ?? "https://localhost:5001";
        }

        public async Task SendEmailConfirmationAsync(string toEmail, string fullName, string confirmationToken)
        {
            var confirmationUrl = $"{_appUrl}/confirm-email?token={Uri.EscapeDataString(confirmationToken)}";

            var subject = "Confirm Your Email - PDXLite";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #F7F4EB; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 15px 30px; background: #00A3A8; color: #FFFFFF !important; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
        .token {{ background: #e0e0e0; padding: 10px; border-radius: 5px; font-family: monospace; word-break: break-all; }}
        .accent-pdx-teal {{color: #00A3A8; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to <span class='accent-pdx-teal'>PDX</span>Lite!</h1>
        </div>
        <div class='content'>
            <h2>Hi {fullName},</h2>
            <p>Thanks for signing up! Please confirm your email address to activate your account and start using PDXLite.</p>
            
            <p style='text-align: center;'>
                <a href='{confirmationUrl}' class='button'>Confirm Email Address</a>
            </p>
            
            <p>Or copy and paste this link into your browser:</p>
            <div class='token'>{confirmationUrl}</div>
            
            <p><strong>This link will expire in 24 hours.</strong></p>
            
            <p>If you didn't create an account with PDXLite, please ignore this email.</p>
            
            <div class='footer'>
                <p>© 2025 PDXLite - PDF Text Extraction with AI</p>
                <p>This is an automated message, please do not reply.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            var plainTextBody = $@"
Hi {fullName},

Thanks for signing up for PDXLite! Please confirm your email address to activate your account.

Confirmation Link:
{confirmationUrl}

This link will expire in 24 hours.

If you didn't create an account with PDXLite, please ignore this email.

---
© 2024 PDXLite - PDF Text Extraction with AI
";

            await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string fullName, string apiKey)
        {
            var subject = "Welcome to PDXLite - Your API Key";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #F7F4EB; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .api-key {{ background: #e8f5e9; border: 2px solid #4caf50; padding: 15px; border-radius: 5px; font-family: monospace; word-break: break-all; margin: 20px 0; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Email Confirmed!</h1>
        </div>
        <div class='content'>
            <h2>Welcome, {fullName}!</h2>
            <p>Your email has been successfully confirmed. Your account is now fully activated!</p>
            
            <h3>Your API Key</h3>
            <div class='api-key'>{apiKey}</div>
            
            <div class='warning'>
                <strong>⚠️ Important:</strong> Keep your API key secure and never share it publicly. Anyone with this key can use your account.
            </div>
            
            <h3>Getting Started</h3>
            <ul>
                <li><strong>Web Interface:</strong> Upload PDFs at {_appUrl}</li>
                <li><strong>API Access:</strong> Use your API key in the X-API-Key header</li>
                <li><strong>Rate Limits:</strong> 1,000 requests per 24 hours</li>
            </ul>
            
            <h3>Quick API Example</h3>
            <pre style='background: #f5f5f5; padding: 15px; border-radius: 5px; overflow-x: auto;'>
curl -X POST {_appUrl}/api/pdf/extract \\
  -H ""X-API-Key: {apiKey}"" \\
  -F ""file=@document.pdf""
            </pre>
            
            <p>Check out our documentation for more examples and features!</p>
            
            <div class='footer'>
                <p>© 2025 PDXLite - PDF Text Extraction with AI</p>
            </div>
        </div>
    </div>
</body>
</html>";

            var plainTextBody = $@"
Welcome, {fullName}!

Your email has been successfully confirmed. Your account is now fully activated!

Your API Key:
{apiKey}

⚠️ IMPORTANT: Keep your API key secure and never share it publicly.

Getting Started:
- Web Interface: {_appUrl}
- API Access: Use your API key in the X-API-Key header
- Rate Limits: 1,000 requests per 24 hours

Quick API Example:
curl -X POST {_appUrl}/api/pdf/extract \
  -H ""X-API-Key: {apiKey}"" \
  -F ""file=@document.pdf""

---
© 2024 PDXLite - PDF Text Extraction with AI
";

            await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string plainTextBody)
        {
            var smtpEnabled = _configuration.GetValue<bool>("Email:Enabled", false);

            if (!smtpEnabled)
            {
                _logger.LogWarning("Email sending is disabled. Email would be sent to: {Email}", toEmail);
                _logger.LogInformation("Email Subject: {Subject}", subject);
                _logger.LogInformation("Email Body (Plain Text):\n{Body}", plainTextBody);
                return;
            }

            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
                var smtpUsername = _configuration["Email:SmtpUsername"];
                var smtpPassword = _configuration["Email:SmtpPassword"];
                var useSsl = _configuration.GetValue<bool>("Email:UseSsl", true);

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SMTP settings not configured. Email not sent to: {Email}", toEmail);
                    return;
                }

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.EnableSsl = useSsl;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);

                // Add plain text alternative
                var plainView = AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain");
                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
                message.AlternateViews.Add(plainView);
                message.AlternateViews.Add(htmlView);

                await client.SendMailAsync(message);

                _logger.LogInformation("Email sent successfully to: {Email}, Subject: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to: {Email}", toEmail);
                // Don't throw - email failure shouldn't break registration
            }
        }
    }
}
