using TickTrader.Algo.Api;

namespace ImportAccountStateBot
{
    public sealed class MarketModeWatcher : OrderBaseWatcher
    {
        public MarketModeWatcher(string symbol, ImportAccountStateBot bot) : base(symbol, bot)
        { }


        protected override bool TryBuildOpenRequest(TransactionToken token, out OpenOrderRequest.Template template)
        {
            template = BuildBaseOpenTemplate(token).WithType(OrderType.Market);

            return true;
        }
    }
}
