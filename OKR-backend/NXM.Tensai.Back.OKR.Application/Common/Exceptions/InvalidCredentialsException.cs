namespace NXM.Tensai.Back.OKR.Application;

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base()
    {
    }

    public InvalidCredentialsException(string message)
        : base(message)
    {
    }

    public InvalidCredentialsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
