using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace _100YearPortfolio
{
    internal sealed class MarketSymbol
    {
        private const int MaxCancelAttempt = 5;
        private const int DelayBetweenFailedRequests = 100;
        
        private const double PercentCoef = 100.0;
        private const OrderType BaseType = OrderType.Limit;

        private readonly PortfolioBot _bot;
        private readonly string _status;
        private readonly bool _isExist;

        public string Name { get; }

        public double Percent { get; }


        private Symbol Symbol => _bot.Symbols[Name];

        private double ExpectedMargin => _bot.CalculationBalance * Percent / PercentCoef;


        public MarketSymbol(PortfolioBot bot, string symbol, double percent)
        {
            _bot = bot;

            Name = symbol;
            Percent = percent;

            if (_bot.Symbols[symbol].IsNull)
                _status = "Not found!";
            else if (percent.Lte(0.0))
                _status = "Percent less than 0!";
            else
            {
                _isExist = true;
                Symbol.Subscribe();
            }
        }


        public string GetCurrentState()
        {
            return _isExist ? $"Current margin = {CalculateMarginDelta():F6}, " +
                              $"expected = {ExpectedMargin:F6}({Percent:F4}%)" : _status;
        }

        public async Task Recalculate()
        {
            if (!_isExist)
                return;

            await CancelOrderChain();

            var margin = ExpectedMargin - CalculateMarginDelta();
            var side = margin.Gte(0.0) ? OrderSide.Buy : OrderSide.Sell;

            _bot.PrintDebug($"Margin delta {Name} = {margin:F6}");

            await OpenOrderChain(side, Math.Abs(margin));
        }

        private double CalculateMarginDelta()
        {
            var buyMargin = _bot.Account.GetSymbolMargin(Name, OrderSide.Buy) ?? 0.0;
            var sellMargin = _bot.Account.GetSymbolMargin(Name, OrderSide.Sell) ?? 0.0;

            return buyMargin - sellMargin;
        }

        private async Task OpenOrderChain(OrderSide side, double margin)
        {
            while (margin.Gte(Symbol.MinTradeVolume))
            {
                if (TryPredicateOpenVolume(side, margin, out var openVolume, out var price))
                {
                    _bot.PrintDebug($"Expected {Name} volume = {openVolume:F6}");

                    var res = await _bot.OpenOrderAsync(BuildRequest(openVolume, price, side));

                    if (res.IsCompleted)
                        margin -= res.ResultingOrder.Margin;
                    else
                        await _bot.Delay(DelayBetweenFailedRequests);
                }
                else
                    break;
            }
        }

        private Task CancelOrderChain()
        {
            async Task Cancel(Order order)
            {
                var attempt = 0;

                while (++attempt < MaxCancelAttempt)
                {
                    var res = await _bot.CancelOrderAsync(order.Id);

                    if (res.IsCompleted)
                        return;
                    else
                        await _bot.Delay(DelayBetweenFailedRequests);
                }
            }

            var orders = _bot.Account.OrdersBySymbol(Name);
            var cancelTasks = new List<Task>(orders.Count);

            foreach (var order in orders)
                cancelTasks.Add(Cancel(order));

            return Task.WhenAll(cancelTasks);
        }

        private bool TryPredicateOpenVolume(OrderSide side, double money, out double volume, out double price)
        {
            var minVolume = Symbol.MinTradeVolume;
            var maxVolume = Symbol.MaxTradeVolume;

            price = GetPrice(side);
            volume = 0.0;

            if (CanOpenOrder(maxVolume, price, side, money))
            {
                volume = maxVolume;
                return true;
            }

            if (!CanOpenOrder(minVolume, price, side, money))
                return false;
            else
                volume = minVolume;

            double step = (maxVolume - minVolume) * 0.5;

            while (step.Gte(Symbol.TradeVolumeStep))
            {
                while (CanOpenOrder(volume + step, price, side, money))
                    volume += step;

                step *= 0.5;
            }

            return true;
        }

        private double GetPrice(OrderSide side)
        {
            return side.IsBuy() ? Symbol.Bid : Symbol.Ask;
        }

        private bool CanOpenOrder(double volume, double? price, OrderSide side, double hasMoney)
        {
            return TryGetMargin(volume, price, side, out var needMoney) && needMoney.Lte(hasMoney);
        }

        private bool TryGetMargin(double volume, double? price, OrderSide side, out double margin)
        {
            price ??= GetPrice(side);

            var result = _bot.Account.CalculateOrderMargin(Name, BaseType, side, volume, null, price, null);
            margin = result ?? 0.0;

            return result is not null;
        }

        private OpenOrderRequest BuildRequest(double volume, double price, OrderSide side)
        {
            return OpenOrderRequest.Template.Create()
                                   .WithParams(Name, side, BaseType, volume, price, null)
                                   .MakeRequest();
        }
    }
}