using SoftFx.Common.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace ImportAccountStateBot
{
    public abstract class OrderBaseWatcher
    {
        private readonly OpenOrderRequest.Template _openRequestTemplate;
        private readonly ConcurrentQueue<TransactionToken> _tokenQueue;

        protected readonly ImportAccountStateBot _bot;
        protected readonly Symbol _symbol;

        private bool _applyInProcess;


        private string SymbolState
        {
            get
            {
                if (_symbol.IsNull)
                    return "not found";

                if (!_symbol.IsTradeAllowed)
                    return "trade is not allowed";

                if (_symbol.LastQuote == null)
                    return "off quotes";

                if (_symbol.LastQuote.IsAskIndicative || _symbol.LastQuote.IsBidIndicative)
                    return "indicative quotes";

                return "ok";
            }
        }

        protected List<Order> Orders => _bot.Account.Orders.Where(u => u.Symbol == _symbol.Name).ToList();

        public bool IsEmpty => !_applyInProcess && _tokenQueue.IsEmpty && Orders.Count == 0;


        protected OrderBaseWatcher(string symbol, ImportAccountStateBot bot)
        {
            _bot = bot;
            _symbol = _bot.Symbols[symbol];

            _tokenQueue = new ConcurrentQueue<TransactionToken>();
            _openRequestTemplate = OpenOrderRequest.Template.Create().WithSymbol(symbol);
        }


        public void PushToken(TransactionToken token)
        {
            if (SymbolState == "ok")
                _tokenQueue.Enqueue(token);
        }

        public void ClearQueue()
        {
            while (!_tokenQueue.IsEmpty)
                if (!_tokenQueue.TryDequeue(out _))
                    break;
        }

        public async Task ApplyToken()
        {
            if (!_applyInProcess)
            {
                _applyInProcess = true;

                var queueToken = GetOptimazeQueueToken();

                if (queueToken != null)
                {
                    var accountToken = GetOptimazeAccountToken();

                    _bot.PrintDebug($"Queue token = {queueToken}, acc token = {accountToken} in apply");

                    await UpdateAccountStateByToken(queueToken + accountToken);
                }

                _applyInProcess = false;
            }
        }

        public async Task CancelOrdersBySide(OrderSide? side = null)
        {
            var cancelList = new List<Task>(1 << 3);
            var orders = side != null ? Orders.Where(u => u.Side == side) : Orders;

            foreach (var order in orders)
                if (!order.IsNull)
                    cancelList.Add(CancelOrderById(order.Id));

            await Task.WhenAll(cancelList);
        }

        public virtual Task CorrectAllOrders() => Task.CompletedTask;


        private TransactionToken GetOptimazeQueueToken()
        {
            if (!_tokenQueue.TryDequeue(out var baseToken))
                return null;

            while (_tokenQueue.TryDequeue(out var nextToken))
                baseToken += nextToken;

            return baseToken;
        }

        private TransactionToken GetOptimazeAccountToken()
        {
            var baseToken = new TransactionToken(_symbol.Name);

            foreach (var order in Orders)
                if (!order.IsNull)
                    baseToken += new TransactionToken(order);

            return baseToken;
        }

        private async Task UpdateAccountStateByToken(TransactionToken token)
        {
            await CancelOrdersBySide(token.IsEmpty ? null : (OrderSide?)token.Side.Inverse());

            var accountToken = GetOptimazeAccountToken();

            _bot.PrintDebug($"Queue token = {token}, acc token = {accountToken} in update");

            if (!accountToken.IsEmpty && accountToken.Volume.Lte(token.Volume))
                await OpenOrderByToken(token - accountToken);
            else
            {
                await CancelOrdersBySide(accountToken.Side);
                await OpenOrderByToken(token);
            }
        }

        private async Task OpenOrderByToken(TransactionToken token)
        {
            while (token.Volume.Gte(_symbol.MinTradeVolume))
            {
                var openToken = TransactionToken.Min(_symbol.MaxTradeVolume, token);

                if (TryBuildOpenRequest(openToken, out var template))
                {
                    var result = await _bot.OpenOrderAsync(template.MakeRequest());

                    if (result.IsCompleted)
                    {
                        var resultingToken = new TransactionToken(result.ResultingOrder);
                        var remainingToken = openToken - resultingToken; // if TTS opened partial order

                        token -= openToken;

                        if (remainingToken.Volume.Gte(_symbol.MinTradeVolume))
                            _tokenQueue.Enqueue(remainingToken);

                        continue;
                    }
                }

                _tokenQueue.Enqueue(token);
                break;
            }
        }

        private async Task CancelOrderById(string id)
        {
            int attempts = 0;

            while (++attempts < 3)
            {
                var response = await _bot.CancelOrderAsync(id);

                if (response.IsFaulted && response.ResultCode != OrderCmdResultCodes.OrderNotFound)
                    await _bot.Delay(100);
            }
        }

        protected abstract bool TryBuildOpenRequest(TransactionToken token, out OpenOrderRequest.Template template);

        protected virtual OpenOrderRequest.Template BuildBaseOpenTemplate(TransactionToken token)
        {
            return _openRequestTemplate.WithSide(token.Side).WithVolume(token.Volume);
        }

        public override string ToString() => $"{_symbol.Name} queue size = {_tokenQueue.Count}, {(_applyInProcess ? "await TTS response, " : "")}symbol status = {SymbolState}!";
    }
}
