namespace NXM.Tensai.Back.OKR.Application;

public class EmailSendException : Exception
{
    public EmailSendException() : base("Email sending failed.")
    {
    }

    public EmailSendException(string message) : base(message)
    {
    }

    public EmailSendException(string message, Exception innerException) : base(message, innerException)
    {
    }
}