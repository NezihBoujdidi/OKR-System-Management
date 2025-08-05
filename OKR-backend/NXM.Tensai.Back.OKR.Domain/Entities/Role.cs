namespace NXM.Tensai.Back.OKR.Domain;

public class Role : IdentityRole<Guid>
{
    // Parameterless constructor for EF Core
    public Role() : base()
    {
    }

    // Constructor with roleName parameter
    public Role(string roleName) : base(roleName)
    {
    }
}
