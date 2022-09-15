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

        private double ActualMoney => _bot.CalculationBalance * Percent / PercentCoef;


        public MarketSymbol(PortfolioBot bot, string symbol, double percent, double? maxLotSum)
        {
            static string SmallValueMessage(string name, double min = 0.0) => $"{name} less than {min}!";

            _bot = bot;

            Name = symbol;
            Percent = percent;
            MaxSumLot = maxLotSum ?? bot.Config.DefaultMaxLotSize;

            if (_bot.Symbols[symbol].IsNull)
                _status = "Not found!";
            else if (MaxSumLot.Lt(Symbol.MinTradeVolume))
                _status = SmallValueMessage(nameof(MaxSumLot), Symbol.MinTradeVolume);
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

            var balance = _bot.CalculationBalance;
            var currentPercent = CalculateMarginDelta() / balance * PercentCoef;
            var delta = Percent - currentPercent;
            var deltaLots = balance * delta / (GetPrice(GetExpectedSide(delta)) * Symbol.ContractSize);

            _sb.Clear()
               .Append($"{nameof(MaxSumLot)} = {MaxSumLot}, ")
               .Append($"expected = {Percent:F4}%, ")
               .Append($"delta = {delta:F4}% ({deltaLots:F4} lots)");

            return _sb.ToString();
        }

        public async Task Recalculate()
        {
            if (!_isExist)
                return;

            await CancelOrderChain();

            var expectedMoney = ActualMoney - CalculateMarginDelta();
            var expectedSide = GetExpectedSide(expectedMoney);

            _bot.PrintDebug($"Expected money {Name} = {expectedMoney:F6}");

            await OpenOrderChain(expectedSide, Math.Abs(expectedMoney));
        }

        private double CalculateMarginDelta()
        {
            var buyMargin = Account.GetSymbolMargin(Name, OrderSide.Buy) ?? 0.0;
            var sellMargin = Account.GetSymbolMargin(Name, OrderSide.Sell) ?? 0.0;

            return buyMargin - sellMargin;
        }

        private async Task OpenOrderChain(OrderSide side, double money)
        {
            var price = GetPrice(side);
            var openVolume = Math.Min(CalculateOpenVolume(money, price), MaxSumLot);

            _bot.PrintDebug($"{Name} open volume = {openVolume}");

            while (openVolume.Gte(Symbol.MinTradeVolume))
            {
                var curVolume = Math.Min(openVolume, Symbol.MaxTradeVolume);
                var attempt = 0;

                while (++attempt < MaxRejectAttempts)
                {
                    var res = await _bot.OpenOrderAsync(BuildRequest(curVolume, price, side));

                    if (res.IsCompleted)
                    {
                        openVolume -= res.ResultingOrder.RemainingVolume;
                        break;
                    }
                    else
                        await _bot.Delay(DelayBetweenFailedRequests);
                }

                if (attempt == MaxRejectAttempts)
                    return;
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

        private double CalculateOpenVolume(double money, double? price = null)
        {
            price ??= GetPrice(GetExpectedSide(money));

            return money / (price.Value * Symbol.ContractSize);
        }

        private double GetPrice(OrderSide side)
        {
            return side.IsBuy() ? Symbol.Bid : Symbol.Ask;
        }

        private OpenOrderRequest BuildRequest(double volume, double price, OrderSide side)
        {
            return OpenOrderRequest.Template.Create()
                                   .WithParams(Name, side, BaseType, volume, price, null)
                                   .WithExpiration(_bot.UtcNow.AddHours(_bot.Config.UpdateMinutes + 1))
                                   .MakeRequest();
        }

        private static OrderSide GetExpectedSide(double money)
        {
            return money.Gte(0.0) ? OrderSide.Buy : OrderSide.Sell;
        }
    }
}