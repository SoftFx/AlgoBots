using SoftFx.Common.Extensions;
using SoftFx.Common.Graphs;
using SoftFx.Common.Graphs.Algorithm;
using System;
using System.Collections.Generic;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace SoftFx.PublicIndicators
{
    [Indicator(DisplayName = "Universal Currency Indicator", Category = CommonConstants.Category, Version = "2.0")]
    public class UniversalCurrencyIndicator : Indicator
    {
        private static readonly TimeSpan _delay = TimeSpan.FromMilliseconds(100);

        private MarketGraph _symbolGraph;
        private PathLogic<CurrencyNode> _pathLogic;
        private int _currencyId;
        private List<int> _currencyListIds;
        private PathSearchResult<CurrencyNode, Edge<CurrencyNode>, double> _lastSearch;
        private DateTime _lastSearchTime;


        [Parameter(DisplayName = "Currency", DefaultValue = "USD")]
        public string Currency { get; set; }

        [Parameter(DisplayName = "Currency List", DefaultValue = "EUR, BTC, JPY")]
        public string CurrencyList { get; set; }


        [Output(DisplayName = "F(currency)", Target = OutputTargets.Window1, DefaultColor = Colors.Red)]
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

            _currencyId = _symbolGraph[Currency]?.Id ?? -1;
            _lastSearchTime = DateTime.Now - _delay - _delay;
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

        protected override void Calculate(bool isNewBar)
        {
            var res = double.NaN;
            if (_currencyId != -1)
            {
                if (_lastSearchTime + _delay < DateTime.Now)
                {
                    var graphSnapshot = _symbolGraph.GetSnapshot(edge => double.IsNaN(edge.ReverseWeight) ? null : new Edge<CurrencyNode>(edge.From, edge.To, edge.ReverseWeight));
                    _lastSearch = BellmanFord<CurrencyNode, Edge<CurrencyNode>>.CalculateShortestPaths(graphSnapshot, _pathLogic, _currencyId);
                    _lastSearchTime = DateTime.Now;
                }

                res = 0;
                foreach (var nodeId in _currencyListIds)
                {
                    res += !_lastSearch.Distance[nodeId].E(_pathLogic.UnreachableValue)
                        ? -_lastSearch.Distance[nodeId]
                        : double.NaN;
                }
            }

            Output[0] = res;
        }
    }
}
