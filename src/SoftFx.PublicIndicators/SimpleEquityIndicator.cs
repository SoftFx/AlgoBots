using SoftFx.Common.Extensions;
using SoftFx.Common.Graphs;
using SoftFx.Common.Graphs.Algorithm;
using System;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace SoftFx.PublicIndicators
{
    [Indicator(DisplayName = "Simple Equity Indicator", Category = CommonConstants.Category, Version = "1.4")]
    public class SimpleEquityIndicator : Indicator
    {
        private static readonly TimeSpan _delay = TimeSpan.FromMilliseconds(100);

        private MarketGraph _symbolGraph;
        private PathLogic<CurrencyNode> _pathLogic;
        private int _currencyId;
        private PathSearchResult<CurrencyNode, Edge<CurrencyNode>, double> _lastSearch;
        private DateTime _lastSearchTime;


        [Parameter(DisplayName = "Base Currency", DefaultValue = "USD")]
        public string BaseCurrency { get; set; }


        [Output(DisplayName = "Equity", Target = OutputTargets.Window1, DefaultColor = Colors.Green)]
        public DataSeries Output { get; set; }


        protected override void Init()
        {
            _symbolGraph = new MarketGraph(this) { Name = "Market graph" };
            _pathLogic = new PathLogic<CurrencyNode>(1000);
            foreach (var symbol in Symbols)
            {
                if (symbol.IsNull || !symbol.IsTradeAllowed)
                    continue;

                var commission = symbol.CalculateCommission(Account.Type, false);
                if (double.IsNaN(commission))
                    commission = 0;
                _symbolGraph.AddEdge(symbol.BaseCurrency, symbol.CounterCurrency, symbol, commission);
                _symbolGraph.AddEdge(symbol.CounterCurrency, symbol.BaseCurrency, symbol, commission);

                symbol.Subscribe();
            }

            _currencyId = _symbolGraph[BaseCurrency]?.Id ?? -1;
            _lastSearchTime = DateTime.Now - _delay -_delay;
        }

        protected override void Calculate(bool isNewBar)
        {
            var res = double.NaN;
            if (_currencyId != -1 && Account.Type == AccountTypes.Cash)
            {
                if (_lastSearchTime + _delay < DateTime.Now)
                {
                    var graphSnapshot = _symbolGraph.GetSnapshot(edge => double.IsNaN(edge.ReverseWeight) ? null : new Edge<CurrencyNode>(edge.From, edge.To, edge.ReverseWeight));
                    _lastSearch = BellmanFord<CurrencyNode, Edge<CurrencyNode>>.CalculateShortestPaths(graphSnapshot, _pathLogic, _currencyId);
                    _lastSearchTime = DateTime.Now;
                }

                res = 0;
                foreach (var asset in Account.Assets)
                {
                    var node = _symbolGraph[asset.Currency];
                    res += (node != null && !_lastSearch.Distance[node.Id].E(_pathLogic.UnreachableValue))
                        ? asset.Volume * Math.Exp(-_lastSearch.Distance[node.Id])
                        : double.NaN;
                }
            }

            Output[0] = res;
        }
    }
}
