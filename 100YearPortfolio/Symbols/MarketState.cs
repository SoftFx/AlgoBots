using _100YearPortfolio.Symbols;
using System.Text;
using TickTrader.Algo.Api.Math;

namespace _100YearPortfolio
{
    internal sealed class MarketState
    {
        private const int DefaultInitialSize = 1 << 4;
        private const double MaxPercentSum = 100.0;

        private readonly Dictionary<string, MarketSymbol> _symbols = new(DefaultInitialSize);
        private readonly List<UnexpectedEntity> _unexpectedSymbols = new(DefaultInitialSize);
        private readonly List<Task> _calculateTasks = new(DefaultInitialSize);

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

            if (HasUnexpectedSymbols())
            {
                sb = GetUnexpectedSection(sb);

                _bot.Alert.Print(sb.ToString());

                sb.AppendLine().AppendLine();
            }

            sb.AppendLine("Symbols:");

            foreach (var symbol in _symbols.Values)
                sb.Append($"{symbol.GetCurrentState()}");

            return sb.ToString();
        }

        private StringBuilder GetUnexpectedSection(StringBuilder sb)
        {
            sb.AppendLine("Unexpected entities:");

            foreach (var entity in _unexpectedSymbols)
                sb.AppendLine($"{entity}");

            sb.Append("Please close them for correct calculations!");

            return sb;
        }

        private bool HasUnexpectedSymbols()
        {
            _unexpectedSymbols.Clear();

            foreach (var pos in _bot.Account.NetPositions)
                if (!_symbols.ContainsKey(pos.Symbol))
                    _unexpectedSymbols.Add(new UnexpectedEntity(pos.Id, pos.Symbol, "NetPostion"));

            foreach (var order in _bot.Account.Orders)
                if (!_symbols.ContainsKey(order.Symbol))
                    _unexpectedSymbols.Add(new UnexpectedEntity(order.Id, order.Symbol, "Order"));

            return _unexpectedSymbols.Count > 0;
        }
    }
}