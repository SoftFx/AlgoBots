using TickTrader.Algo.Api;

namespace SoftFx.Common.Extensions
{
    public static class QuoteExtensions
    {
        /// <summary>
        /// Returns best bid or best ask according to side
        /// </summary>
        /// <returns>Best price</returns>
        public static double BestPrice(this Quote quote, OrderSide side)
        {
            return side == OrderSide.Buy ? quote.Ask : quote.Bid;
        }
    }
}
