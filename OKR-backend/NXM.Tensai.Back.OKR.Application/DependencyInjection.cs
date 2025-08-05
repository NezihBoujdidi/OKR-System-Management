namespace NXM.Tensai.Back.OKR.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddValidators()
            .AddMediator()
            .AddServices();
        return services;
    }

    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()));

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {

        return services;
    }
}
