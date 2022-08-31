using System.Text;
using TickTrader.Algo.Api.Math;

namespace _100YearPortfolio
{
    internal sealed class MarketState
    {
        private const double MaxPercentSum = 100.0;

        private readonly List<MarketSymbol> _symbols = new(1 << 4);
        private readonly List<Task> _calculateTasks = new(1 << 4);


        public Task Recalculate()
        {
            for (int i = 0; i < _symbols.Count; i++)
                _calculateTasks[i] = _symbols[i].Recalculate();

            return Task.WhenAll(_calculateTasks);
        }

        public void AddSymbol(MarketSymbol symbol)
        {
            _symbols.Add(symbol);
            _calculateTasks.Add(Task.CompletedTask);
        }

        public bool CheckTotalPercent()
        {
            return _symbols.Sum(u => u.Percent).Lte(MaxPercentSum);
        }

        public string BuildCurrentState()
        {
            var sb = new StringBuilder(1 << 10);

            foreach (var symbol in _symbols)
                sb.AppendLine($"{symbol.Name} - {symbol.GetCurrentState()}");

            return sb.ToString();
        }
    }
}