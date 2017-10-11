using SoftFx.Common.BusinessObjects;
using SoftFx.Common.Extensions;
using SoftFx.Common.Graphs;
using SoftFx.Common.Graphs.Algorithm;
using System;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace SoftFx.PublicIndicators
{
    [Indicator(DisplayName = "Simple Equity Indicator", Category = CommonConstants.Category, Version = "1.2")]
    public class SimpleEquityIndicator : Indicator
    {
        private BarMarketGraph _symbolGraph;
        private PathLogic<CurrencyNode> _pathLogic;
        private int _currencyId;


        [Parameter(DisplayName = "Base Currency", DefaultValue = "USD")]
        public string BaseCurrency { get; set; }


        [Output(DisplayName = "Equity", Target = OutputTargets.Window1, DefaultColor = Colors.Green)]
        public DataSeries Output { get; set; }


        protected override void Init()
        {
            _symbolGraph = new BarMarketGraph(this) { Name = "Bar market graph" };
            _pathLogic = new PathLogic<CurrencyNode>(1000);
            foreach (var symbol in Symbols)
            {
                if (symbol.IsNull || !symbol.IsTradeAllowed)
                    continue;

                var barSymbol = new BarSymbol(symbol, this);
                var commission = symbol.CalculateCommission(Account.Type, false);
                if (double.IsNaN(commission))
                    commission = 0;
                _symbolGraph.AddEdge(symbol.BaseCurrency, symbol.CounterCurrency, barSymbol, commission);
                _symbolGraph.AddEdge(symbol.CounterCurrency, symbol.BaseCurrency, barSymbol, commission);
            }

            _currencyId = _symbolGraph[BaseCurrency]?.Id ?? -1;
        }

        protected override void Calculate()
        {
            var res = double.NaN;
            if (_currencyId != -1 && Account.Type == AccountTypes.Cash)
            {
                var graphSnapshot = _symbolGraph.GetSnapshot(edge => double.IsNaN(edge.ReverseWeight) ? null : new Edge<CurrencyNode>(edge.From, edge.To, edge.ReverseWeight));
                var searchResult = BellmanFord<CurrencyNode, Edge<CurrencyNode>>.CalculateShortestPaths(graphSnapshot, _pathLogic, _currencyId);

                res = 0;
                foreach (var asset in Account.Assets)
                {
                    var node = _symbolGraph[asset.Currency];
                    res += (node != null && !searchResult.Distance[node.Id].E(_pathLogic.UnreachableValue))
                        ? asset.Volume * Math.Exp(-searchResult.Distance[node.Id])
                        : double.NaN;
                }
            }

            Output[0] = res;
        }
    }
}
