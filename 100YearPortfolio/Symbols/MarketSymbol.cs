using System.Text;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace _100YearPortfolio
{
    internal sealed class MarketSymbol
    {
        private const int MaxRejectAttempts = 5;
        private const int DelayBetweenFailedRequests = 100;

        private const OrderType BaseType = OrderType.Limit;

        internal const double PercentCoef = 100.0;

        private readonly StringBuilder _sb = new(1 << 10);
        private readonly PortfolioBot _bot;
        private readonly string _status;
        private readonly bool _isExist;

        public string Name { get; }

        public double Percent { get; }

        public double MaxSumLot { get; }


        private Symbol Symbol => _bot.Symbols[Name];

        private AccountDataProvider Account => _bot.Account;

        private NetPosition Position => Account.NetPositions[Name];


        private double ActualMoney => _bot.CalculationBalance * Percent / PercentCoef;


        public MarketSymbol(PortfolioBot bot, string symbol, double percent, double? maxLotSum)
        {
            _bot = bot;

            Name = symbol;
            Percent = percent;
            MaxSumLot = maxLotSum ?? bot.Config.DefaultMaxLotSize;

            if (_bot.Symbols[symbol].IsNull)
                _status = "Not found!";
            else if (MaxSumLot.Lt(Symbol.MinTradeVolume))
                _status = $"{nameof(MaxSumLot)} less than {Symbol.MinTradeVolume}!";
            else
            {
                _isExist = true;

                Symbol.Subscribe();
            }
        }


        public string GetCurrentState()
        {
            if (!_isExist)
                return _status;

            var used = GetUsedMoney(out var bid, out var ask);
            var deltaMoney = ActualMoney - used;
            var deltaLots = CalculateOpenVolume(deltaMoney, deltaMoney > 0 ? bid : ask);
            var deltaPercent = Percent - used / _bot.CalculationBalance * PercentCoef;

            _sb.Clear()
               .Append($"{nameof(MaxSumLot)} = {MaxSumLot}, ")
               .Append($"expected = {Percent:F2}%, ")
               .Append($"delta = {deltaPercent:F2}% ({deltaLots:0.#####} lots)");

            if (_bot.UseDebug)
                _sb.Append($", rate {bid}/{ask}");

            return _sb.ToString();
        }

        public async Task Recalculate()
        {
            if (!_isExist)
                return;

            await CancelOrderChain();

            var expectedMoney = ActualMoney - GetUsedMoney(out var bid, out var ask);

            _bot.PrintDebug($"{Name} money delta = {expectedMoney:F6}");

            await OpenOrderChain(expectedMoney, expectedMoney > 0 ? bid : ask);
        }

        private async Task OpenOrderChain(double money, double price)
        {
            var expectedVolume = Math.Min(CalculateOpenVolume(Math.Abs(money), price), MaxSumLot);
            var expectedSide = money.Gte(0.0) ? OrderSide.Buy : OrderSide.Sell;

            _bot.PrintDebug($"{Name} expected volume = {expectedVolume:F8}, min volume = {Symbol.MinTradeVolume}");
            _bot.PrintDebug($"{Name} try open = {expectedVolume.Gte(Symbol.MinTradeVolume)}");

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

            var orders = Account.OrdersBySymbol(Name);
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

            foreach (var order in Account.OrdersBySymbol(Name))
            {
                money += order.RemainingVolume * (order.Side.IsBuy() ? bid : -ask);
            }

            return money * Symbol.ContractSize;
        }

        private OpenOrderRequest BuildRequest(double volume, double price, OrderSide side)
        {
            return OpenOrderRequest.Template.Create()
                                   .WithParams(Name, side, BaseType, volume, price, null)
                                   .WithExpiration(_bot.UtcNow.AddHours(_bot.Config.UpdateMinutes + 1))
                                   .MakeRequest();
        }
    }
}