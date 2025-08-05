namespace NXM.Tensai.Back.OKR.Application;

public class RefreshTokenResponse
{
    public string Token { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime Expires { get; set; }
}
