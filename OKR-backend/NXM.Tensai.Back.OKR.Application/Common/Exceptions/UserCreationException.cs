namespace NXM.Tensai.Back.OKR.Application;

public class UserCreationException : Exception
{
    public UserCreationException() : base("User creation failed.")
    {
    }

    public UserCreationException(string message) : base(message)
    {
    }

    public UserCreationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
