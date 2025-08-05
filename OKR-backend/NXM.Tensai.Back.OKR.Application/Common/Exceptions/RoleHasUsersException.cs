namespace NXM.Tensai.Back.OKR.Application;

public class RoleHasUsersException : Exception
{
    public RoleHasUsersException() : base("Cannot delete role because it has users assigned to it.")
    {
    }

    public RoleHasUsersException(string message) : base(message)
    {
    }

    public RoleHasUsersException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
