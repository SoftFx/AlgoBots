using SoftFx.Common.Extensions;
using SoftFx.Common.Graphs;
using SoftFx.Common.Graphs.Algorithm;
using System.Collections.Generic;
using System.Threading.Tasks;
using TickTrader.Algo.Api;

namespace SoftFx.PublicIndicators
{
    [Indicator(DisplayName = "Universal Currency Indicator", Category = CommonConstants.Category, Version = "1.0")]
    public class UniversalCurrencyIndicator : Indicator
    {
        private MarketGraph _symbolGraph;
        private PathLogic<CurrencyNode> _pathLogic;
        private bool _isRunning;
        private PathSearchResult<CurrencyNode, Edge<CurrencyNode>, double> _searchResult;
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
            RecalculateSearchResult(_searchResult != null);

            var res = double.NaN;
            if (_currencyId != -1)
            {
                res = 0;
                foreach (var nodeId in _currencyListIds)
                {
                    res += -_searchResult.Distance[nodeId];
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
