using SoftFx.Common.Extensions;
using SoftFx.Common.Graphs;
using SoftFx.Common.Graphs.Algorithm;
using System;
using System.Threading.Tasks;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace SoftFx.PublicIndicators
{
    [Indicator(DisplayName = "Simple Equity Indicator", Category = CommonConstants.Category, Version = "1.0")]
    public class SimpleEquityIndicator : Indicator
    {
        private MarketGraph _symbolGraph;
        private PathLogic<CurrencyNode> _pathLogic;
        private bool _isRunning;
        private PathSearchResult<CurrencyNode, Edge<CurrencyNode>, double> _searchResult;
        private int _currencyId;


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
        }

        protected override void Calculate()
        {
            RecalculateSearchResult(_searchResult != null);

            var res = double.NaN;
            if (_currencyId != -1 && Account.Type == AccountTypes.Cash)
            {
                res = 0;
                foreach (var asset in Account.Assets)
                {
                    var node = _symbolGraph[asset.Currency];
                    if (node != null && !_searchResult.Distance[node.Id].E(_pathLogic.UnreachableValue))
                    {
                        res += asset.Volume * Math.Exp(-_searchResult.Distance[node.Id]);
                    }
                }
            }

            Output[0] = res;
        }


        private async void RecalculateSearchResult(bool delay)
        {
            if (_isRunning || _currencyId == -1)
                return;

            _isRunning = true;

            if (delay)
            {
                await Task.Delay(50);
            }
            var graphSnapshot = _symbolGraph.GetSnapshot(edge => new Edge<CurrencyNode>(edge.From, edge.To, edge.ReverseWeight));
            _searchResult = BellmanFord<CurrencyNode, Edge<CurrencyNode>>.CalculateShortestPaths(graphSnapshot, _pathLogic, _currencyId);

            _isRunning = false;
        }
    }
}
