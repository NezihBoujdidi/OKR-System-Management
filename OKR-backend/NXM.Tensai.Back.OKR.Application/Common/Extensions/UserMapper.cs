using NXM.Tensai.Back.OKR.Domain;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace NXM.Tensai.Back.OKR.Application;

public static class UserMapper
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            SupabaseId = user.SupabaseId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Address = user.Address,
            Email = user.Email!,
            Position= user.Position,
            DateOfBirth = user.DateOfBirth,
            ProfilePictureUrl = user.ProfilePictureUrl,
            IsNotificationEnabled = user.IsNotificationEnabled,
            IsEnabled = user.IsEnabled,
            Gender = user.Gender,
            OrganizationId = user.OrganizationId,
        };
    }

    public static IEnumerable<UserDto> ToDto(this IEnumerable<User> users)
    {
        return users.Select(user => user.ToDto()).ToList();
    }
    
    public static UserWithRoleDto ToUserWithRoleDto(this User user, string role)
    {
        return new UserWithRoleDto
        {
            Id = user.Id,
            SupabaseId = user.SupabaseId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Address = user.Address,
            Email = user.Email!,
            Position = user.Position,
            DateOfBirth = user.DateOfBirth,
            ProfilePictureUrl = user.ProfilePictureUrl,
            IsNotificationEnabled = user.IsNotificationEnabled,
            IsEnabled = user.IsEnabled,
            Gender = user.Gender,
            OrganizationId = user.OrganizationId,
            Role = role,
            CreatedDate = user.CreatedDate,
            ModifiedDate = user.ModifiedDate,
        };
    }

    public static User ToUser(this CreateUserCommand userCommand)
    {
        if (userCommand == null) throw new ArgumentNullException(nameof(userCommand));

        return new User
        {
            SupabaseId = userCommand.SupabaseId ?? string.Empty,
            FirstName = userCommand.FirstName,
            LastName = userCommand.LastName,
            UserName = userCommand.Email,
            Email = userCommand.Email,
            Address = userCommand.Address,
            Position = userCommand.Position,
            DateOfBirth = userCommand.DateOfBirth.Kind == DateTimeKind.Utc
                ? userCommand.DateOfBirth
                : DateTime.SpecifyKind(userCommand.DateOfBirth, DateTimeKind.Utc),
            IsEnabled = userCommand.IsEnabled,
            Gender = userCommand.Gender
        };
    }

    public static void UpdateEntity(this UpdateUserCommand command, User entity)
    {
        entity.FirstName = command.FirstName;
        entity.LastName = command.LastName;
        entity.UserName = command.Email;
        entity.Email = command.Email;
        entity.Address = command.Address;
        entity.Position = command.Position;
        // Ensure DateOfBirth is UTC
        entity.DateOfBirth = command.DateOfBirth.Kind == DateTimeKind.Utc
            ? command.DateOfBirth
            : DateTime.SpecifyKind(command.DateOfBirth, DateTimeKind.Utc);

        entity.ProfilePictureUrl = command.ProfilePictureUrl;
        entity.IsNotificationEnabled = command.IsNotificationEnabled;
        entity.IsEnabled = command.IsEnabled;
        entity.Gender = command.Gender;
        entity.OrganizationId = command.OrganizationId;
        entity.ModifiedDate = DateTime.UtcNow;
    }

    public static User ToEntity(this RegisterUserCommand command)
    {
        return new User
        {
            SupabaseId = command.SupabaseId,
            FirstName = command.FirstName,
            LastName = command.LastName,
            UserName = command.Email,
            Email = command.Email,
            Address = command.Address,
            Position = command.Position,
            DateOfBirth = command.DateOfBirth.Kind == DateTimeKind.Utc
                ? command.DateOfBirth
                : DateTime.SpecifyKind(command.DateOfBirth, DateTimeKind.Utc),
            Gender = command.Gender,
            IsEnabled = command.IsEnabled,
            OrganizationId = command.OrganizationID,
            PhoneNumber = command.PhoneNumber
        };
    }
}
