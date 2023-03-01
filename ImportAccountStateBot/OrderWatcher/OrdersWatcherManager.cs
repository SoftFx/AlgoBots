using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ImportAccountStateBot
{
    public sealed class OrderWatchersManager
    {
        private readonly Dictionary<string, OrderBaseWatcher> _watchers;
        private readonly ImportAccountStateBot _bot;


        public OrderWatchersManager(ImportAccountStateBot bot)
        {
            _watchers = new Dictionary<string, OrderBaseWatcher>();
            _bot = bot;
        }


        public void GetTokenHandler(TransactionToken token)
        {
            var watcher = GetOrCreateWatcher(token.Symbol);

            _bot.PrintDebug($"Received {token}");

            watcher.PushToken(token);
        }

        public async Task ApplyAllTokens()
        {
            var applyList = new List<Task>(_watchers.Keys.Count);

            foreach (var symbol in _watchers.Keys)
                applyList.Add(_watchers[symbol].ApplyToken());

            await Task.WhenAll(applyList);
        }

        public async Task CorrectAllOrders()
        {
            var correctList = new List<Task>(_watchers.Keys.Count);

            foreach (var symbol in _watchers.Keys)
                correctList.Add(_watchers[symbol].CorrectAllOrders());

            await Task.WhenAll(correctList);
        }

        public async Task ClearAllWatchers()
        {
            var cancelList = new List<Task>(1 << 3);

            foreach (var symbol in _watchers.Keys)
            {
                _watchers[symbol].ClearQueue();
                cancelList.Add(_watchers[symbol].CancelOrdersBySide());
            }

            await Task.WhenAll(cancelList);
        }

        public bool AllWatchersIsEmpty()
        {
            var isEmpty = true;

            foreach (var symbol in _watchers.Keys)
                isEmpty &= _watchers[symbol].IsEmpty;

            return isEmpty;
        }

        private OrderBaseWatcher GetOrCreateWatcher(string symbol)
        {
            if (!_watchers.TryGetValue(symbol, out var watcher))
            {
                switch (_bot.Config.Mode)
                {
                    case ImportMode.Market:
                        watcher = new MarketModeWatcher(symbol, _bot);
                        break;
                    case ImportMode.TrailingLimit:
                    case ImportMode.TrailingLimitPercent:
                        watcher = new TralingLimitModeWatcher(symbol, _bot);
                        break;
                }

                _watchers.Add(symbol, watcher);
            }

            return watcher;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(1 << 10);

            sb.AppendLine($"Symbols state:");

            foreach (var symbol in _watchers.Keys)
                sb.AppendLine($"{_watchers[symbol]}");

            return sb.ToString();
        }
    }
}
