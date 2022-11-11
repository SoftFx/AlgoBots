using System.Text;
using TickTrader.Algo.Api.Math;

namespace _100YearPortfolio
{
    internal sealed class MarketState
    {
        private const double MaxPercentSum = 100.0;

        private readonly Dictionary<string, MarketSymbol> _symbols = new(1 << 4);
        private readonly List<Task> _calculateTasks = new(1 << 4);


        public Task Recalculate()
        {
            var smb = _symbols.Values.ToList();

            for (int i = 0; i < smb.Count; i++)
                _calculateTasks[i] = smb[i].Recalculate();

            return Task.WhenAll(_calculateTasks);
        }

        public bool AddSymbol(MarketSymbol symbol)
        {
            var ok = _symbols.TryAdd(symbol.Name, symbol);

            if (ok)
                _calculateTasks.Add(Task.CompletedTask);

            return ok;
        }

        public bool CheckTotalPercent()
        {
            return _symbols.Values.Sum(u => Math.Abs(u.Percent)).Lte(MaxPercentSum);
        }

        public override string ToString()
        {
            var sb = new StringBuilder(1 << 10);

            sb.AppendLine("Symbols:");

            foreach (var symbol in _symbols.Values)
                sb.AppendLine($"{symbol.Name} - {symbol.GetCurrentState()}");

            return sb.ToString();
        }
    }
}