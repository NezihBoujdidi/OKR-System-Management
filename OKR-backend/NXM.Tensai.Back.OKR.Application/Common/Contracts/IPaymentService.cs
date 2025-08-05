namespace NXM.Tensai.Back.OKR.Application;

public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentMethod paymentMethod, decimal amount, Guid userId);
}
