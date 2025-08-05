namespace NXM.Tensai.Back.OKR.Application;

public class UserDto
{
    public Guid Id { get; set; }
    public string SupabaseId { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string Email { get; set; } = null!;
    public Role? Role { get; set; } 
    public string Position { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public bool IsNotificationEnabled { get; set; }
    public bool IsEnabled { get; set; }
    public Gender Gender { get; set; }
    public Guid? OrganizationId { get; set; }
}
