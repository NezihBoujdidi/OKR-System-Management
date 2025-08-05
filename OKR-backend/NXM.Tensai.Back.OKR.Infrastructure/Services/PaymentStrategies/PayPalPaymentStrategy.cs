using NXM.Tensai.Back.OKR.Application;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class PayPalPaymentStrategy : IPaymentStrategy
{
    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, Guid userId)
    {
        // Simulate PayPal payment processing
        await Task.Delay(1000); // Simulate delay
        return new PaymentResult(
            true,
            Guid.NewGuid().ToString()
        );
    }
}
