using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class SeedDataHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SeedDataHostedService> _logger;

    public SeedDataHostedService(
        IServiceProvider serviceProvider,
        ILogger<SeedDataHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<OKRDbContext>();
                
                // Ensure database is created and all migrations are applied
                _logger.LogInformation("Ensuring database is up to date...");
                await context.Database.MigrateAsync(cancellationToken);
                
                // Try seeding with retries
                const int maxRetries = 3;
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        _logger.LogInformation("Attempting to seed data (attempt {Attempt} of {MaxRetries})...", i + 1, maxRetries);
                        await SeedData.InitializeAsync(services);
                        
                        // Update OrganizationAdmin permissions
                        var rolePermissionService = services.GetRequiredService<RolePermissionUpdateService>();
                        await rolePermissionService.UpdateRolePermissions("OrganizationAdmin");
                        
                        _logger.LogInformation("Data seeding and permission updates completed successfully.");
                        break;
                    }
                    catch (Exception ex) when (i < maxRetries - 1)
                    {
                        _logger.LogWarning(ex, "Data seeding failed on attempt {Attempt}. Retrying...", i + 1);
                        await Task.Delay(2000, cancellationToken); // Wait 2 seconds before retry
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during database initialization and seeding.");
                throw; // Rethrow to prevent application startup if database init fails
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
