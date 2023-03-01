using TickTrader.Algo.Api;

namespace SoftFx.Common.Extensions
{
    public static class OrderSideExtensions
    {
        public static OrderSide Inverse(this OrderSide side)
        {
            return side.IsBuy() ? OrderSide.Sell : OrderSide.Buy;
        }
    }
}
