namespace NXM.Tensai.Back.OKR.Application;

public class PasswordResetException : Exception
{
    public PasswordResetException() : base("Password reset failed.")
    {
    }

    public PasswordResetException(string message) : base(message)
    {
    }

    public PasswordResetException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
