namespace stocks_core.DTOs.AverageTradedPrice
{
    public class AverageTradedPriceCalculator
    {
        public AverageTradedPriceCalculator(double currentPrice, double currentQuantity)
        {
            CurrentPrice = currentPrice;
            CurrentQuantity = currentQuantity;
        }

        public double CurrentPrice { get; set; }
        public double CurrentQuantity { get; set; }
        public double AverageTradedPrice { get; set; } = 0;
    }
}
