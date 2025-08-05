namespace NXM.Tensai.Back.OKR.Application;

public interface IPaymentStrategy
{
    Task<PaymentResult> ProcessPaymentAsync(decimal amount, Guid userId);
}
