namespace NXM.Tensai.Back.OKR.Domain;

public class User : IdentityUser<Guid>
{
    public string SupabaseId { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; } = string.Empty;
    public bool IsNotificationEnabled { get; set; }
    public string Position { get; set; } = null!; //Software dev - UI/UX ...  
    public bool IsEnabled { get; set; }
    public Gender Gender { get; set; }
    public Guid? OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public DateTime CreatedDate { get; private set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
}
