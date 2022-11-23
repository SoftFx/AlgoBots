using System.Text;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace _100YearPortfolio
{
    internal sealed record NoteSettings
    {
        internal string SymbolOrigin { get; init; }

        internal double? MaxLotSize { get; init; }
    }


    internal sealed class MarketSymbol
    {
        private const OrderType BaseType = OrderType.Limit;

        private const int MaxRejectAttempts = 5;
        private const int DelayBetweenFailedRequests = 100;
        private const double PercentCoef = 100.0;

        private readonly StringBuilder _sb = new(1 << 10);
        private readonly PortfolioBot _bot;

        private readonly string _error;


        public string OriginName { get; }

        public string Alias { get; }

        public double Percent { get; }

        public double MaxLotSize { get; }


        private Symbol Symbol => _bot.Symbols[OriginName];

        private AccountDataProvider Account => _bot.Account;

        private NetPosition Position => Account.NetPositions[OriginName];


        private double ActualMoney => _bot.CalculationBalance * Percent / PercentCoef;


        public MarketSymbol(PortfolioBot bot, string aliasName, double percent, NoteSettings settings)
        {
            _bot = bot;

            Alias = aliasName;
            Percent = percent;

            OriginName = settings.SymbolOrigin ?? aliasName;
            MaxLotSize = settings.MaxLotSize ?? Symbol.MaxTradeVolume;

            if (!Symbol.IsNull)
            {
                if (MaxLotSize.Lt(Symbol.MinTradeVolume))
                    _error = $"{nameof(MaxLotSize)} less than MinTradeVolume = {Symbol.MinTradeVolume}!";
                else
                    Symbol.Subscribe();
            }
        }


        public string GetCurrentState()
        {
            if (Symbol.IsNull)
                return string.Empty;

            if (!string.IsNullOrEmpty(_error))
                return _error;

            var used = GetUsedMoney(out var bid, out var ask);

            if (Percent.E(0.0) && used.E(0.0))
                return string.Empty;

            var deltaMoney = ActualMoney - used;
            var deltaPercent = Percent - used / _bot.CalculationBalance * PercentCoef;

            var openVolume = CalculateOpenVolume(deltaMoney, deltaMoney > 0 ? bid : ask);

            _sb.Clear()
               .Append($"{Alias}{(Alias != OriginName ? $"({OriginName})" : "")} - ")
               .Append($"{nameof(MaxLotSize)} = {MaxLotSize}, ")
               .Append($"expected = {Percent:F2}%, ")
               .Append($"delta = {deltaPercent:F2}% ({openVolume:0.#####} lots)");

            if (_bot.UseDebug)
                _sb.Append($", rate {bid}/{ask}");

            return _sb.AppendLine().ToString();
        }

        public async Task Recalculate()
        {
            if (Symbol.IsNull)
                return;

            await CancelOrderChain();

            var expectedMoney = ActualMoney - GetUsedMoney(out var bid, out var ask);

            _bot.PrintDebug($"{OriginName} money delta = {expectedMoney:F6}");

            await OpenOrderChain(expectedMoney, expectedMoney > 0 ? bid : ask);
        }

        private async Task OpenOrderChain(double money, double price)
        {
            var expectedVolume = Math.Min(CalculateOpenVolume(Math.Abs(money), price), MaxLotSize);
            var expectedSide = money.Gte(0.0) ? OrderSide.Buy : OrderSide.Sell;

            _bot.PrintDebug($"{OriginName} expected volume = {expectedVolume:F8}, min volume = {Symbol.MinTradeVolume}");
            _bot.PrintDebug($"{OriginName} try open = {expectedVolume.Gte(Symbol.MinTradeVolume)}");

            if (expectedVolume.Gte(Symbol.MinTradeVolume))
            {
                expectedVolume = Math.Min(expectedVolume, Symbol.MaxTradeVolume);

                var attempt = 0;

                while (++attempt < MaxRejectAttempts)
                {
                    var res = await _bot.OpenOrderAsync(BuildRequest(expectedVolume, price, expectedSide));

                    if (res.IsCompleted)
                        break;
                    else
                        await _bot.Delay(DelayBetweenFailedRequests);
                }
            }
        }

        private Task CancelOrderChain()
        {
            async Task Cancel(Order order)
            {
                var attempt = 0;

                while (++attempt < MaxRejectAttempts)
                {
                    var res = await _bot.CancelOrderAsync(order.Id);

                    if (res.IsCompleted)
                        return;
                    else
                        await _bot.Delay(DelayBetweenFailedRequests);
                }
            }

            var orders = Account.OrdersBySymbol(OriginName);
            var cancelTasks = new List<Task>(orders.Count);

            foreach (var order in orders)
                cancelTasks.Add(Cancel(order));

            return Task.WhenAll(cancelTasks);
        }

        private double CalculateOpenVolume(double money, double price)
        {
            return money / (price * Symbol.ContractSize);
        }

        private double GetUsedMoney(out double bid, out double ask)
        {
            bid = Symbol.Bid;
            ask = Symbol.Ask;

            var money = Position.Volume * (Position.Side.IsBuy() ? bid : -ask);

            foreach (var order in Account.OrdersBySymbol(OriginName))
            {
                money += order.RemainingVolume * (order.Side.IsBuy() ? bid : -ask);
            }

            return money * Symbol.ContractSize;
        }

        private OpenOrderRequest BuildRequest(double volume, double price, OrderSide side)
        {
            return OpenOrderRequest.Template.Create()
                                   .WithParams(OriginName, side, BaseType, volume, price, null)
                                   .WithExpiration(_bot.UtcNow.AddHours(_bot.Config.UpdateMinutes + 1))
                                   .MakeRequest();
        }
    }
}