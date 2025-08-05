using SendGrid.Helpers.Mail;
using SendGrid;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var client = new SendGridClient(apiKey);
        var fromEmail = _configuration["SendGrid:FromEmail"];
        var fromName = _configuration["SendGrid:FromName"];
        var from = new EmailAddress(fromEmail, fromName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);

        var response = await client.SendEmailAsync(msg);
        if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            throw new EmailException($"Failed to send email to {toEmail}. Status code: {response.StatusCode}");
        }
    }
}
