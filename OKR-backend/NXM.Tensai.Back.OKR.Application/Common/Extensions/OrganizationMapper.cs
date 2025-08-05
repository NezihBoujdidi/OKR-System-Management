namespace NXM.Tensai.Back.OKR.Application;

public static class OrganizationMapper
{
    public static Organization ToEntity(this CreateOrganizationCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        return new Organization
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
            Country = command.Country,
            Industry = command.Industry,
            Email = command.Email,
            Phone = command.Phone,
            CreatedDate = DateTime.UtcNow,
            Size = command.Size,
            IsActive = command.IsActive,
        };
    }

    public static void UpdateEntity(this UpdateOrganizationCommand command, Organization entity)
    {
        entity.Name = command.Name;
        entity.Description = command.Description;
        entity.Country = command.Country;
        entity.Industry = command.Industry;
        entity.Email = command.Email;
        entity.Phone = command.Phone;
        entity.Size = command.Size; 
        if (command.IsActive.HasValue)
        {
            entity.IsActive = command.IsActive.Value;
        }
        
        entity.ModifiedDate = DateTime.UtcNow;
    }

    public static OrganizationDto ToDto(this Organization organization, string? subscriptionPlan = null)
    {
        if (organization == null) throw new ArgumentNullException(nameof(organization));

        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Description = organization.Description,
            Country = organization.Country,
            Industry = organization.Industry,
            Email = organization.Email,
            Phone = organization.Phone,
            Size = organization.Size,
            IsActive = organization.IsActive,
            CreatedDate = organization.CreatedDate,
            ModifiedDate = organization.ModifiedDate,
            SubscriptionPlan = subscriptionPlan // Set plan if provided
        };
    }
}
