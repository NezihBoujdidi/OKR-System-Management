namespace NXM.Tensai.Back.OKR.Infrastructure;

public class SupabaseJwtSettings
{
    public string JwtSecret { get; set; } = null!;
    public string PublicKey { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
}