namespace NXM.Tensai.Back.OKR.Application;

public class EmailConfirmationException : Exception
{
    public EmailConfirmationException() : base("Email confirmation failed.")
    {
    }

    public EmailConfirmationException(string message) : base(message)
    {
    }

    public EmailConfirmationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
