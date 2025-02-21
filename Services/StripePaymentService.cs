using Stripe.Checkout;

namespace ProRota.Services
{
    public class StripePaymentService
    {
        public async Task<string> CreateCheckoutSession(decimal amount, string currency, string successUrl, string cancelUrl, string plan)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = (long)(amount * 100), // Stripe expects amount in cents
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "ProRota Subscription", 
                            Description = $"Sevice: {plan} site license"
                        }
                    },
                    Quantity = 1
                }
            },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return session.Url; // Returns Stripe Checkout URL
        }
    }
}
