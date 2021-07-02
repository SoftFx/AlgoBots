using TickTrader.Algo.Api;

namespace SoftFx.Common.Extensions
{
    public static class OrderSideExtensions
    {
        public static bool IsBuy(this OrderSide side)
        {
            return side == OrderSide.Buy;
        }

        public static bool IsSell(this OrderSide side)
        {
            return side == OrderSide.Sell;
        }

        public static OrderSide Inverse(this OrderSide side)
        {
            return side.IsBuy() ? OrderSide.Sell : OrderSide.Buy;
        }
    }
}
