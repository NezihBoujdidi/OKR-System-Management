namespace NXM.Tensai.Back.OKR.Application;

public class GenerateInvitationLinkCommand : IRequest
{
    public string Email { get; init; } = null!;
    public string RoleName { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public Guid? TeamId { get; set; }
}

public class GenerateInvitationLinkCommandValidator : AbstractValidator<GenerateInvitationLinkCommand>
{
    public GenerateInvitationLinkCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.");
            
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID is required.");
    }

}

public class GenerateInvitationLinkCommandHandler : IRequestHandler<GenerateInvitationLinkCommand>
{
    private readonly IInvitationLinkRepository _invitationLinkRepository;
    private readonly IEmailSender _emailSender;
    private readonly IValidator<GenerateInvitationLinkCommand> _validator;
    private readonly IJwtService _jwtService;
    private readonly RoleManager<Role> _roleManager;

    public GenerateInvitationLinkCommandHandler(
        IInvitationLinkRepository invitationLinkRepository,
        IEmailSender emailSender,
        IValidator<GenerateInvitationLinkCommand> validator,
        IJwtService jwtService,
        RoleManager<Role> roleManager) 
    {
        _invitationLinkRepository = invitationLinkRepository;
        _emailSender = emailSender;
        _validator = validator;
        _jwtService = jwtService;
        _roleManager = roleManager;
    }

    public async Task Handle(GenerateInvitationLinkCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var role = await _roleManager.FindByNameAsync(request.RoleName);
        if (role == null)
        {
            throw new KeyNotFoundException($"Role with name {request.RoleName} not found.");
        }

        // Generate JWT Token using the JwtService with user, role, organization ID and team ID if present
        var expirationDate = DateTime.UtcNow.AddHours(24); // Or get from config if needed
        var token = await _jwtService.GenerateJwtToken(
            new User { Email = request.Email }, 
            request.RoleName, 
            request.OrganizationId,
            request.TeamId);

        // Save the token in the database 
        var invitationLink = new InvitationLink
        {
            Email = request.Email,
            Role = role,
            Token = token,
            OrganizationId = request.OrganizationId,
            TeamId = request.TeamId, // Store TeamId in the invitation link
            ExpirationDate = expirationDate,
            CreatedAt = DateTime.UtcNow
        };

        await _invitationLinkRepository.AddAsync(invitationLink);

        // Create the invitation link URL
        var invitationLinkUrl = $"http://localhost:4200/signup?token={token}";

        // Enhanced Email body with yellow/black style and styled button
        var emailBody = $@"
            <div style='background-color:#fffbe6;padding:32px 24px;border-radius:12px;max-width:480px;margin:0 auto;font-family:Segoe UI,Arial,sans-serif;color:#222;box-shadow:0 2px 8px rgba(0,0,0,0.07);'>
                <h2 style='color:#222;margin-top:0;margin-bottom:16px;'>You're Invited!</h2>
                <p style='margin:0 0 18px 0;'>Hello,</p>
                <p style='margin:0 0 18px 0;'>You have been invited to join our OKR platform. Please click the button below to complete your registration:</p>
                <div style='text-align:center;margin:32px 0;'>
                    <a href='{invitationLinkUrl}' style='background-color:#ffd600;color:#222;text-decoration:none;padding:14px 32px;border-radius:6px;font-weight:bold;font-size:16px;display:inline-block;box-shadow:0 2px 4px rgba(0,0,0,0.08);border:2px solid #222;transition:background 0.2s;'>
                        Complete your registration
                    </a>
                </div>
                <p style='margin:0 0 12px 0;font-size:14px;color:#444;'>If you did not request this invitation, please ignore this email.</p>
                <p style='margin:0;font-size:14px;color:#888;'>Best regards,<br><span style='color:#222;font-weight:bold;'>OKR Support Team</span></p>
            </div>
        ";

        // Send the invitation email
        await _emailSender.SendEmailAsync(request.Email, "Invitation to join our platform", emailBody);
    }
}
