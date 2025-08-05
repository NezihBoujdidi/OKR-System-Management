namespace NXM.Tensai.Back.OKR.Application;

public class RoleAssignmentException : Exception
{
    public RoleAssignmentException() : base("Role assignment failed.")
    {
    }

    public RoleAssignmentException(string message) : base(message)
    {
    }

    public RoleAssignmentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
