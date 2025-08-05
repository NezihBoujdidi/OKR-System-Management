namespace NXM.Tensai.Back.OKR.Infrastructure;

public class EmailException : Exception
{
    public EmailException(string message) : base(message) { }

    public EmailException(string message, Exception innerException)
        : base(message, innerException) { }
}
