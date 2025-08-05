using Microsoft.AspNetCore.Builder;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseSupabaseAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SupabaseAuthenticationMiddleware>();
    }
}