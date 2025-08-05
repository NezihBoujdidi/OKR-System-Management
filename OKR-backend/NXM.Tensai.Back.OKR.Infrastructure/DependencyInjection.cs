using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NXM.Tensai.Back.OKR.Application;
using System.Security.Claims;
using System.Text;
using NXM.Tensai.Back.OKR.Application.Features.Documents.Interfaces;
using NXM.Tensai.Back.OKR.Infrastructure.Services;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using NXM.Tensai.Back.OKR.Infrastructure.Repositories;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddPersistence(configuration)
            .AddAuthenticationAndAuthorization(configuration)
            .AddExternalServices(configuration);

        // Add the seed method call here
        services.AddHostedService<SeedDataHostedService>();

        return services;
    }

    public static IServiceCollection ConfigureDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OKRDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<User, Role>(options => 
        { 
            //email confirmation handled by Supabase
            options.SignIn.RequireConfirmedEmail = false;
        })
            .AddEntityFrameworkStores<OKRDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureDatabases(configuration);
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleClaimsRepository, RoleClaimsRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IOKRSessionRepository, OKRSessionRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IObjectiveRepository, ObjectiveRepository>();
        services.AddScoped<IKeyResultRepository, KeyResultRepository>();
        services.AddScoped<IKeyResultTaskRepository, KeyResultTaskRepository>();
        services.AddScoped<IInvitationLinkRepository, InvitationLinkRepository>();
        services.AddScoped<IOKRSessionTeamRepository, OKRSessionTeamRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ITeamUserRepository, TeamUserRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IOKRStatsService, OKRStatsService>();
        services.AddScoped<ICollaboratorPerformanceService, CollaboratorPerformanceService>();
        services.AddScoped<IEmployeeGrowthStatsService, EmployeeGrowthStatsService>();
        services.AddScoped<ITeamPerformanceService, TeamPerformanceService>();
        services.AddScoped<ICollaboratorTaskStatusStatsService, CollaboratorTaskStatusStatsService>();
        services.AddScoped<ICollaboratorMonthlyPerformanceService, CollaboratorMonthlyPerformanceService>();
        services.AddScoped<IActiveTeamsService, ActiveTeamsService>();
        services.AddScoped<IManagerSessionStatsService, ManagerSessionStatsService>();
        services.AddScoped<IGlobalStatsService, GlobalStatsService>();
        services.AddScoped<ICollaboratorTaskDetailsService, CollaboratorTaskDetailsService>();
        services.AddScoped<ISubscriptionAnalyticsService, SubscriptionAnalyticsService>();
        services.AddScoped<IManagerObjectiveService, ManagerObjectiveService>();
        return services;
    }

    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure JWT settings from configuration
        var jwtSettings = new JwtSettings();
        configuration.GetSection("JwtSettings").Bind(jwtSettings);
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // Configure Supabase JWT settings
        var supabaseJwtSettings = new SupabaseJwtSettings();
        configuration.GetSection("SupabaseJwtSettings").Bind(supabaseJwtSettings);
        services.Configure<SupabaseJwtSettings>(configuration.GetSection("SupabaseJwtSettings"));

        // Register Supabase JWT service
        services.AddScoped<SupabaseJwtService>();

        // Configure authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // Set token validation parameters for both local and Supabase JWTs
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = supabaseJwtSettings.Issuer,
                ValidAudience = supabaseJwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseJwtSettings.JwtSecret)),
                ClockSkew = TimeSpan.Zero
            };

            // Process events for enhanced token validation
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    // Token is already validated by the JwtBearer middleware
                    var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                    var supabaseJwtService = context.HttpContext.RequestServices.GetRequiredService<SupabaseJwtService>();
                    
                    // Extract the Supabase user ID from the token
                    var supabaseUserId = supabaseJwtService.GetSupabaseUserId(context.Principal);
                    if (string.IsNullOrEmpty(supabaseUserId))
                    {
                        context.Fail("Invalid token: missing Supabase user ID");
                        return;
                    }

                    // Find the user in our database
                    var user = await userRepository.GetUserBySupabaseIdAsync(supabaseUserId);
                    if (user == null)
                    {
                        context.Fail("User not found in the system");
                        return;
                    }

                    // Add the SupabaseId claim if it's not already there
                    if (!context.Principal.HasClaim(c => c.Type == "SupabaseId"))
                    {
                        var identity = context.Principal.Identity as ClaimsIdentity;
                        identity?.AddClaim(new Claim("SupabaseId", supabaseUserId));
                    }
                },
                
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },

                OnChallenge = context =>
                {
                    // Skip the default logic if we're handling the failure ourselves
                    if (context.AuthenticateFailure != null)
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var result = System.Text.Json.JsonSerializer.Serialize(new { error = "Unauthorized" });
                        return context.Response.WriteAsync(result);
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

            // Add policies for each permission
            options.AddPolicy(Permissions.AccessAll, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.AccessAll)));
            options.AddPolicy(Permissions.Users_Create, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Users_Create)));
            options.AddPolicy(Permissions.Users_Update, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Users_Update)));
            options.AddPolicy(Permissions.Users_GetById, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Users_GetById)));
            options.AddPolicy(Permissions.Users_GetAll, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Users_GetAll)));
            options.AddPolicy(Permissions.Users_Invite, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Users_Invite)));
            options.AddPolicy(Permissions.Users_GetByEmail, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Users_GetByEmail)));
            options.AddPolicy(Permissions.Users_GetByOrganizationId, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Users_GetByOrganizationId)));
            options.AddPolicy(Permissions.Users_GetTeamManagersByOrganizationId, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Users_GetTeamManagersByOrganizationId)));
            options.AddPolicy(Permissions.Roles_Create, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Roles_Create)));
            options.AddPolicy(Permissions.Roles_Update, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Roles_Update)));
            options.AddPolicy(Permissions.Roles_Delete, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Roles_Delete)));
            options.AddPolicy(Permissions.Roles_GetById, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Roles_GetById)));
            options.AddPolicy(Permissions.Roles_GetAll, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Roles_GetAll)));
            options.AddPolicy(Permissions.UsersRoles_Assign, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.UsersRoles_Assign)));
            options.AddPolicy(Permissions.UsersRoles_Remove, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.UsersRoles_Remove)));
            options.AddPolicy(Permissions.UsersRoles_GetAll, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.UsersRoles_GetAll)));
            options.AddPolicy(Permissions.Organizations_Create, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Organizations_Create)));
            options.AddPolicy(Permissions.Organizations_Update, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Organizations_Update)));
            options.AddPolicy(Permissions.Organizations_Delete, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Organizations_Delete)));
            options.AddPolicy(Permissions.Organizations_GetById, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Organizations_GetById)));
            options.AddPolicy(Permissions.Organizations_GetAll, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Organizations_GetAll)));
            options.AddPolicy(Permissions.Objectives_Create, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Objectives_Create)));
            options.AddPolicy(Permissions.Objectives_Update, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Objectives_Update)));
            options.AddPolicy(Permissions.Objectives_Delete, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Objectives_Delete)));
            options.AddPolicy(Permissions.Objectives_GetById, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Objectives_GetById)));
            options.AddPolicy(Permissions.Objectives_GetAll, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Objectives_GetAll)));
            options.AddPolicy(Permissions.KeyResults_Create, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.KeyResults_Create)));
            options.AddPolicy(Permissions.KeyResults_Update, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.KeyResults_Update)));
            options.AddPolicy(Permissions.KeyResults_Delete, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.KeyResults_Delete)));
            options.AddPolicy(Permissions.KeyResults_GetById, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.KeyResults_GetById)));
            options.AddPolicy(Permissions.KeyResults_GetAll, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.KeyResults_GetAll)));
            options.AddPolicy(Permissions.KeyResultTasks_Create, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.KeyResultTasks_Create)));
            options.AddPolicy(Permissions.KeyResultTasks_Update, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.KeyResultTasks_Update)));
            options.AddPolicy(Permissions.KeyResultTasks_Delete, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.KeyResultTasks_Delete)));
            options.AddPolicy(Permissions.KeyResultTasks_GetById, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.KeyResultTasks_GetById)));
            options.AddPolicy(Permissions.KeyResultTasks_GetAll, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.KeyResultTasks_GetAll)));
            options.AddPolicy(Permissions.OKRSessions_Create, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.OKRSessions_Create)));
            options.AddPolicy(Permissions.OKRSessions_Update, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.OKRSessions_Update)));
            options.AddPolicy(Permissions.OKRSessions_Delete, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.OKRSessions_Delete)));
            options.AddPolicy(Permissions.OKRSessions_GetById, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.OKRSessions_GetById)));
            options.AddPolicy(Permissions.OKRSessions_GetAll, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.OKRSessions_GetAll)));
            options.AddPolicy(Permissions.Teams_Create, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Teams_Create)));
            options.AddPolicy(Permissions.Teams_Update, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Teams_Update)));
            options.AddPolicy(Permissions.Teams_Delete, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Teams_Delete)));
            options.AddPolicy(Permissions.Teams_GetById, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Teams_GetById)));
            options.AddPolicy(Permissions.Teams_GetAll, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Teams_GetAll)));
            options.AddPolicy(Permissions.Teams_GetByOrganizationId, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Teams_GetByOrganizationId)));
            options.AddPolicy(Permissions.Teams_GetByCollaboratorId, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Teams_GetByCollaboratorId)));
            options.AddPolicy(Permissions.Teams_GetByManagerId, policy => policy.Requirements.Add(new PermissionRequirement(Permissions.Teams_GetByManagerId)));
        });

        // Register the custom authorization handler
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();

        return services;
    }

    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind Stripe settings
        var stripeSettings = new StripeSettings();
        configuration.GetSection("StripeSettings").Bind(stripeSettings);
        services.Configure<StripeSettings>(configuration.GetSection("StripeSettings"));

        // Register Stripe settings as a singleton
        services.AddSingleton(stripeSettings);

        services.AddScoped<IPaymentStrategy, CreditCardPaymentStrategy>();
        services.AddScoped<IPaymentStrategy, PayPalPaymentStrategy>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<ISubscriptionAnalyticsService, SubscriptionAnalyticsService>();

        services.AddScoped<IPaymentService, PaymentService>(provider =>
        {
            var strategies = new Dictionary<PaymentMethod, IPaymentStrategy>
            {
                { PaymentMethod.CreditCard, provider.GetRequiredService<CreditCardPaymentStrategy>() },
                { PaymentMethod.PayPal, provider.GetRequiredService<PayPalPaymentStrategy>() }
            };

            return new PaymentService(strategies);
        });

        // Register the Supabase client
        services.AddScoped<ISupabaseClient, SupabaseClient>();

        services.AddTransient<IJwtService, JwtService>();
        services.AddTransient<IEmailSender, EmailSender>();
        services.AddScoped<RolePermissionUpdateService>();

        // Register document services
        services.AddScoped<IDocumentStorageService, LocalDocumentStorageService>();

        return services;
    }
}
