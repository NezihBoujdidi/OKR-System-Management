namespace NXM.Tensai.Back.OKR.Infrastructure;

public class OrganizationRepository : Repository<Organization>, IOrganizationRepository
{
    public OrganizationRepository(OKRDbContext context) : base(context)
    {
    }

    // Add any specific methods for Organization if needed
}
