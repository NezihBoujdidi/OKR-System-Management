namespace NXM.Tensai.Back.OKR.Domain;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string message);
}
