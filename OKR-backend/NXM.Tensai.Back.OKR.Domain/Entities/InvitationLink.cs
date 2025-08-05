namespace NXM.Tensai.Back.OKR.Domain;
public class InvitationLink
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public Guid RoleId { get; set; } // Foreign key to Role table
    public Guid? TeamId { get; set; }
    public virtual Role Role { get; set; } = null!;
    public DateTime ExpirationDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
