using Microsoft.Extensions.Options;
using NXM.Tensai.Back.OKR.Application;
using Stripe;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class CreditCardPaymentStrategy : IPaymentStrategy
{
    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, Guid userId)
    {
        try
        {
            var stripeSettings = new OptionsWrapper<StripeSettings>(new StripeSettings());
            var stripeSecretKey = stripeSettings.Value.SecretKey;
            StripeConfiguration.ApiKey = stripeSecretKey;

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Stripe expects the amount in cents
                Currency = "usd",
                PaymentMethodTypes = new List<string> { "card" },
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            return new PaymentResult(
                success: true,
                transactionId: paymentIntent.Id
            );
        }
        catch (StripeException ex)
        {
            return new PaymentResult(
                success: false,
                transactionId: null!,
                errorMessage: ex.Message
            );
        }
    }
}
