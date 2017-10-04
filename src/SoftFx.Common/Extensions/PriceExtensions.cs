using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace SoftFx.Common.Extensions
{
    public static class PriceExtensions
    {
        /// <summary>
        /// Calculates price worsen by commission according to side
        /// </summary>
        /// <returns>Worsen price</returns>
        public static double ApplyCommission(this double price, double commission, OrderSide side)
        {
            return side == OrderSide.Sell ? price * (1 - commission) : price / (1 - commission);
        }

        /// <summary>
        /// Calculates price worsen by markup according to side
        /// </summary>
        /// <returns>Worsen price</returns>
        public static double ApplyMarkup(this double price, double markup, OrderSide side)
        {
            return side == OrderSide.Sell ? price * (1 - markup) : price * (1 + markup);
        }

        /// <summary>
        /// Calculates price worsen by slippage according to side
        /// </summary>
        /// <returns>Worsen price</returns>
        public static double ApplySlippage(this double price, double slippage, OrderSide side)
        {
            return side == OrderSide.Sell ? price * (1 - slippage) : price * (1 + slippage);
        }

        /// <summary>
        /// Determines one price is better than other according to side
        /// </summary>
        public static bool IsBetterPrice(this double price, double otherPrice, OrderSide side)
        {
            return side == OrderSide.Sell ? price.Gt(otherPrice) : price.Lt(otherPrice);
        }
    }
}
