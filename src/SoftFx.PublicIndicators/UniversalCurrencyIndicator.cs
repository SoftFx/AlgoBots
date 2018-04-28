using SoftFx.Common.BusinessObjects;
using SoftFx.Common.Extensions;
using SoftFx.Common.Graphs;
using SoftFx.Common.Graphs.Algorithm;
using System.Collections.Generic;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace SoftFx.PublicIndicators
{
    [Indicator(DisplayName = "Universal Currency Indicator", Category = CommonConstants.Category, Version = "1.3")]
    public class UniversalCurrencyIndicator : Indicator
    {
        private BarMarketGraph _symbolGraph;
        private PathLogic<CurrencyNode> _pathLogic;
        private int _currencyId;
        private List<int> _currencyListIds;


        [Parameter(DisplayName = "Currency", DefaultValue = "USD")]
        public string Currency { get; set; }

        [Parameter(DisplayName = "Currency List", DefaultValue = "EUR, BTC, JPY")]
        public string CurrencyList { get; set; }


        [Output(DisplayName = "F(currency)", Target = OutputTargets.Window1, DefaultColor = Colors.Red)]
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

            _currencyId = _symbolGraph[Currency]?.Id ?? -1;
            _currencyListIds = new List<int>();
            foreach (var currency in CurrencyList.ParseCsvLine())
            {
                var node = _symbolGraph[currency];
                if (node != null)
                {
                    _currencyListIds.Add(node.Id);
                }
            }

        }

        protected override void Calculate()
        {
            var res = double.NaN;
            if (_currencyId != -1)
            {
                var graphSnapshot = _symbolGraph.GetSnapshot(edge => double.IsNaN(edge.ReverseWeight) ? null : new Edge<CurrencyNode>(edge.From, edge.To, edge.ReverseWeight));
                var searchResult = BellmanFord<CurrencyNode, Edge<CurrencyNode>>.CalculateShortestPaths(graphSnapshot, _pathLogic, _currencyId);

                res = 0;
                foreach (var nodeId in _currencyListIds)
                {
                    res += !searchResult.Distance[nodeId].E(_pathLogic.UnreachableValue)
                        ? -searchResult.Distance[nodeId]
                        : double.NaN;
                }
            }

            Output[0] = res;
        }
    }
}
