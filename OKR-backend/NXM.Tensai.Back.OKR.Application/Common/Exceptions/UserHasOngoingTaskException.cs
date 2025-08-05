namespace NXM.Tensai.Back.OKR.Application;

public class UserHasOngoingTaskException : Exception
{
    public UserHasOngoingTaskException() : base("User has an ongoing task. Please reassign it before moving the user to another team.")
    {
    }

    public UserHasOngoingTaskException(string message) : base(message)
    {
    }

    public UserHasOngoingTaskException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
