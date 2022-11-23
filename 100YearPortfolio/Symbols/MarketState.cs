using System.Text;
using TickTrader.Algo.Api.Math;

namespace _100YearPortfolio
{
    internal sealed class MarketState
    {
        private const double MaxPercentSum = 100.0;

        private readonly Dictionary<string, MarketSymbol> _symbols = new(1 << 4);
        private readonly List<Task> _calculateTasks = new(1 << 4);

        private readonly PortfolioBot _bot;


        internal MarketState(PortfolioBot bot)
        {
            _bot = bot;
        }


        public Task Recalculate()
        {
            var smb = _symbols.Values.ToList();

            for (int i = 0; i < smb.Count; i++)
                _calculateTasks[i] = smb[i].Recalculate();

            return Task.WhenAll(_calculateTasks);
        }

        public bool AddSymbol(string name, string alias, double percent, NoteSettings settings)
        {
            if (_symbols.ContainsKey(name))
                return false;

            var symbol = new MarketSymbol(_bot, alias, percent, settings);

            if (_symbols.TryAdd(symbol.OriginName, symbol))
                _calculateTasks.Add(Task.CompletedTask);

            return true;
        }

        public bool CheckTotalPercent(out string distribution)
        {
            distribution = string.Join("; ", _symbols.Values.Select(u => u.Percent));

            return _symbols.Values.Sum(u => Math.Abs(u.Percent)).Lte(MaxPercentSum);
        }

        public override string ToString()
        {
            var sb = new StringBuilder(1 << 10);

            sb.AppendLine("Symbols:");

            foreach (var symbol in _symbols.Values)
                sb.Append($"{symbol.GetCurrentState()}");

            return sb.ToString();
        }
    }
}