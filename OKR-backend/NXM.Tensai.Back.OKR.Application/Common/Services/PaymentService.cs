namespace NXM.Tensai.Back.OKR.Application;

public class PaymentService : IPaymentService
{
    private readonly IDictionary<PaymentMethod, IPaymentStrategy> _paymentStrategies;

    public PaymentService(IDictionary<PaymentMethod, IPaymentStrategy> paymentStrategies)
    {
        _paymentStrategies = paymentStrategies;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentMethod paymentMethod, decimal amount, Guid userId)
    {
        if (!_paymentStrategies.TryGetValue(paymentMethod, out var strategy))
        {
            throw new NotSupportedException($"Payment method {paymentMethod} is not supported.");
        }

        return await strategy.ProcessPaymentAsync(amount, userId);
    }
}
