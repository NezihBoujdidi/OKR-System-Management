var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Add AI services
builder.Services.AddAIServices(builder.Configuration);

// Configure DbContext
// builder.Services.AddDbContext<OKRDbContext>(options =>
//     options.UseNpgsql(
//         builder.Configuration.GetConnectionString("DefaultConnection"),
//         b => b.MigrationsAssembly(typeof(OKRDbContext).Assembly.FullName)));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "NXM Tensai Back OKR API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NXM Tensai Back OKR API v1");
    c.RoutePrefix = string.Empty;
});

// Enable static files
app.UseStaticFiles();

app.UseRouting();

// Only redirect to HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Add CORS middleware
app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Add the Supabase authentication middleware before the Authentication and Authorization middleware
app.UseSupabaseAuthentication();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the Program class accessible for integration tests
public partial class Program { }
