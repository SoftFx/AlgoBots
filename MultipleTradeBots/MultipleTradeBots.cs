using System.Threading.Tasks;
using TickTrader.Algo.Api;

namespace MultipleTradeBots
{
    [TradeBot(Category = "My bots", DisplayName = "MultipleTradeBots 1 part", Version = "1.0",
        Description = "My awesome MultipleTradeBots")]
    public class MultipleTradeBots : TradeBot
    {
        private Task _tradeTask;


        [Parameter(DisplayName = "Position Side", DefaultValue = OrderSide.Buy)]
        public OrderSide PositionSide { get; set; }

        [Parameter(DisplayName = "Volume", DefaultValue = 0.2)]
        public double Volume { get; set; }

        [Parameter(DisplayName = "Time to wait", DefaultValue = 1000)]
        public int TimeToWait { get; set; }


        protected override void OnStart()
        {
            if (Account.Type != AccountTypes.Net)
            {
                PrintError("This bot is designed to work only on net accounts");
                Exit();
                return;
            }
            if (TimeToWait < 100)
            {
                PrintError("Delay is too small");
                Exit();
                return;
            }

            _tradeTask = TradeLoop();
        }

        protected override async Task AsyncStop()
        {
            Print("Stop requested. Awaiting trade loop");

            if (_tradeTask != null)
                await _tradeTask;
        }

        protected async Task TradeLoop()
        {
            var positionOpenRequest = OpenOrderRequest.Template.Create()
                .WithParams(Symbol.Name, PositionSide, OrderType.Market, Volume, null, null)
                .MakeRequest();
            var positionCloseRequest = OpenOrderRequest.Template.Create()
                .WithParams(Symbol.Name, PositionSide == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy, OrderType.Market, Volume, null, null)
                .MakeRequest();

            while (!IsStopped)
            {
                await OpenOrderAsync(positionOpenRequest);
                await Delay(TimeToWait);
                await OpenOrderAsync(positionCloseRequest);
            }
        }
    }
}
