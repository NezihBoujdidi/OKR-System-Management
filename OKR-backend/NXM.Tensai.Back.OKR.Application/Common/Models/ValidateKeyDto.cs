namespace NXM.Tensai.Back.OKR.Application;

public class ValidateKeyDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpirationDate { get; set; }
}
