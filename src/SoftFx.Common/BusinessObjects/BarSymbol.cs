using SoftFx.Common.Adapters;
using System;
using TickTrader.Algo.Api;

namespace SoftFx.Common.BusinessObjects
{
    public class BarSymbol : Symbol
    {
        public Symbol ApiSymbol { get; }

        public BarSeries BidBars { get; }

        public BarSeries AskBars { get; }


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


        #region Symbol implementation

        public string Name => ApiSymbol.Name;

        public int Digits => ApiSymbol.Digits;

        public double Point => ApiSymbol.Point;

        public double ContractSize => ApiSymbol.ContractSize;

        public double MaxTradeVolume => ApiSymbol.MaxTradeVolume;

        public double MinTradeVolume => ApiSymbol.MinTradeVolume;

        public double TradeVolumeStep => ApiSymbol.TradeVolumeStep;

        public bool IsTradeAllowed => ApiSymbol.IsTradeAllowed;

        public bool IsNull => ApiSymbol.IsNull;

        public string BaseCurrency => ApiSymbol.BaseCurrency;

        public Currency BaseCurrencyInfo => ApiSymbol.BaseCurrencyInfo;

        public string CounterCurrency => ApiSymbol.CounterCurrency;

        public Currency CounterCurrencyInfo => ApiSymbol.CounterCurrencyInfo;

        public double Bid => BidBars[0].Close;

        public double Ask => AskBars[0].Close;

        public Quote LastQuote => new QuoteAdapter(Name, BidBars[0], AskBars[0]);

        public double Commission => ApiSymbol.Commission;

        public double LimitsCommission => ApiSymbol.LimitsCommission;

        public CommissionChargeMethod CommissionChargeMethod => ApiSymbol.CommissionChargeMethod;

        public CommissionChargeType CommissionChargeType => ApiSymbol.CommissionChargeType;

        public CommissionType CommissionType => ApiSymbol.CommissionType;


        public void Subscribe(int depth = 1)
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe()
        {
            throw new NotImplementedException();
        }

        #endregion Symbol implementation
    }
}
