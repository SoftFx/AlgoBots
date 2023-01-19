using System.Runtime.CompilerServices;
using System.Text;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace _100YearPortfolio
{
    internal sealed record NoteSettings
    {
        internal string SymbolOrigin { get; init; }

        internal double? MaxOrderSize { get; init; }
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

        public double MaxOrderSize { get; }


        private Symbol Symbol => _bot.Symbols[OriginName];

        private AccountDataProvider Account => _bot.Account;

        private NetPosition Position => Account.NetPositions[OriginName];


        private string FullName => $"{Alias}{(Alias != OriginName ? $"({OriginName})" : "")}";

        private double ActualMoney => _bot.CalculationBalance * Percent;

        private double MinLotSize => Symbol.MinTradeVolume;


        public MarketSymbol(PortfolioBot bot, string aliasName, double percent, NoteSettings settings)
        {
            _bot = bot;

            Alias = aliasName;
            Percent = percent;

            OriginName = settings.SymbolOrigin ?? aliasName;
            MaxOrderSize = settings.MaxOrderSize ?? Symbol.MaxTradeVolume;

            if (!Symbol.IsNull)
            {
                if (MaxOrderSize.Lt(MinLotSize))
                    _error = $"{nameof(MaxOrderSize)} less than MinTradeVolume = {MinLotSize}!";
                else
                    Symbol.Subscribe();
            }
            else
                _bot.PrintError($"Symbol {FullName} not found!");
        }


        public string GetCurrentState()
        {
            if (Symbol.IsNull)
                return $"{FullName} - Symbol not found!\n";

            if (!string.IsNullOrEmpty(_error))
                return _error;

            var used = CalculateUsedMoney(out var bid, out var ask);

            if (Percent.E(0.0) && used.E(0.0))
                return string.Empty;

            var deltaMoney = ActualMoney - used;
            var deltaPercent = Percent - used / _bot.CalculationBalance;

            var openVolume = CalculateVolume(deltaMoney, deltaMoney > 0 ? bid : ask);

            if (openVolume.Lt(Symbol.MinTradeVolume))
                openVolume = 0.0;

            _sb.Clear()
               .Append($"{FullName} - ")
               .Append($"Expected = {Percent * PercentCoef:F2}%, ")
               .Append($"delta = {deltaPercent * PercentCoef:F2}% ({openVolume:0.#####} lots), ")
               .Append($"rate {bid}/{ask}, ")
               .Append($"{nameof(MaxOrderSize)} = {MaxOrderSize}");

            return _sb.AppendLine().ToString();
        }

        public async Task Recalculate()
        {
            if (Symbol.IsNull)
                return;

            await CancelOrder();

            var expectedMoney = ActualMoney - CalculateUsedMoney(out var bid, out var ask);

            _bot.Print($"{OriginName} money delta = {expectedMoney:F6}");

            await OpenOrder(expectedMoney, expectedMoney > 0 ? bid : ask);
        }

        private Task OpenOrder(double money, double price)
        {
            var expectedVolume = Math.Min(CalculateVolume(Math.Abs(money), price), MaxOrderSize).Round(Symbol.TradeVolumeStep);
            var expectedSide = money.Gte(0.0) ? OrderSide.Buy : OrderSide.Sell;

            _bot.Print($"{OriginName} expected volume = {expectedVolume:F8}, min volume = {MinLotSize}");
            _bot.Print($"{OriginName} try open = {expectedVolume.Gte(MinLotSize)}");

            return expectedVolume.Gte(MinLotSize) ?
                   ExecuteRequest(() => _bot.OpenOrderAsync(BuildRequest(expectedVolume, price, expectedSide))) :
                   Task.CompletedTask;
        }

        private Task CancelOrder()
        {
            var orders = Account.OrdersBySymbol(OriginName);
            var cancelTasks = new List<Task>(orders.Count);

            foreach (var order in orders)
            {
                var orderId = order.Id;

                cancelTasks.Add(ExecuteRequest(() => _bot.CancelOrderAsync(orderId)));
            }

            return Task.WhenAll(cancelTasks);
        }

        private double CalculateVolume(double money, double price)
        {
            return money / (price * Symbol.ContractSize);
        }

        private double CalculateUsedMoney(out double bid, out double ask)
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

        private async Task ExecuteRequest(Func<Task<OrderCmdResult>> request, [CallerMemberName] string action = "")
        {
            var attempt = 0;
            OrderCmdResult result = null;

            while (++attempt < MaxRejectAttempts)
            {
                result = await request();

                if (result.IsCompleted)
                    return;
                else
                    await _bot.Delay(DelayBetweenFailedRequests);
            }

            _bot.Alert.Print($"Cannot {action} {FullName}. Error = {result?.ResultCode}");
        }

        private OpenOrderRequest BuildRequest(double volume, double price, OrderSide side)
        {
            return OpenOrderRequest.Template.Create()
                                   .WithParams(OriginName, side, BaseType, volume, price, null)
                                   .WithExpiration(_bot.UtcNow.AddMinutes(_bot.Config.UpdateMinutes + 1))
                                   .MakeRequest();
        }
    }
}