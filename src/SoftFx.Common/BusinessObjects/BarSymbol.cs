using SoftFx.Common.Extensions;
using System;
using TickTrader.Algo.Api;

namespace SoftFx.Common.BusinessObjects
{
    public class BarSymbol
    {
        public Symbol ApiSymbol { get; }

        public BarSeries BidBars { get; }

        public BarSeries AskBars { get; }

        public string Name => ApiSymbol.Name;

        public string BaseCurrency => ApiSymbol.BaseCurrency;

        public string CounterCurrency => ApiSymbol.CounterCurrency;

        public double Bid => BidBars[0].Close;

        public double Ask => AskBars[0].Close;


        public BarSymbol(string symbol, AlgoPlugin plugin) : this(plugin.Symbols[symbol], plugin) { }

        public BarSymbol(Symbol symbol, AlgoPlugin plugin)
        {
            if (plugin == null)
                throw new ArgumentException("Plugin cannot be null");
            if (symbol?.IsNull ?? true)
                throw new ArgumentException("Symbol can't be null");

            ApiSymbol = symbol;
            BidBars = plugin.Feed.GetBarSeries(symbol.Name, BarPriceType.Bid);
            AskBars = plugin.Feed.GetBarSeries(symbol.Name, BarPriceType.Ask);
        }


        public bool HasValidQuotes(OrderSide side)
        {
            return side == OrderSide.Buy
                ? !AskBars[0].IsNull && !double.IsNaN(Ask)
                : !BidBars[0].IsNull && !double.IsNaN(Bid);
        }

        public double BestPrice(OrderSide side)
        {
            return side == OrderSide.Buy ? Ask : Bid;
        }

        public double RoundPrice(double price, OrderSide side, int extraDigits = 0)
        {
            return ApiSymbol.RoundPrice(price, side, extraDigits);
        }
    }
}
