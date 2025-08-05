namespace NXM.Tensai.Back.OKR.Application;

public class UserNotFoundException : Exception
{
    public UserNotFoundException() : base("User not found.")
    {
    }

    public UserNotFoundException(string message) : base(message)
    {
    }

    public UserNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
