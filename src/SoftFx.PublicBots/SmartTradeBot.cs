using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TickTrader.Algo.Api;

namespace SmartTradeBot
{
    [TradeBot(Category = "SoftFX Private", DisplayName = "SmartTradeBot", Version = "1.0")]
    public class SmartTradeBot : TradeBot
    {
        [Parameter(DisplayName = "Volume", DefaultValue = 1)]
        public double Volume { get; set; }

        [Parameter(DisplayName = "Order side", DefaultValue = OrderSide.Buy)]
        public OrderSide Side { get; set; }

        private double price;
        private string limitOrderId;
        private List<double> fillVolume = new List<double>();
        private List<double> fillPrice = new List<double>();

        protected override void Init()
        {
            Feed.Subscribe(Symbol.Name, 2);
            price = (Side == OrderSide.Buy) ? Symbol.Bid : Symbol.Ask;
            Account.Orders.Filled += OrderFilledEvent;
            limitOrderId = OpenOrder(Symbol.Name, OrderType.Limit, Side, Volume, price).ResultingOrder.Id;
        }

        protected override void OnQuote(Quote quote)
        {
            price = GetCurrentPrice(quote);
            var limitOrder = Account.Orders[limitOrderId];

            if (limitOrder != null && price != limitOrder.Price)
                ModifyOrder(limitOrderId, price);
        }

        private BookEntry[] GetBookEntry(Quote quote)
        {
            return (Side == OrderSide.Buy) ? quote.BidBook : quote.AskBook;
        }

        private double GetCurrentPrice(Quote quote)
        {
            BookEntry[] book = GetBookEntry(quote);

            if (price == book[0].Price && Volume == book[0].Volume)
                return book[1].Price;
            else
                return book[0].Price;
        }

        private void OrderFilledEvent(OrderFilledEventArgs args)
        {

            if (args.OldOrder.Id == limitOrderId)
            {
                fillPrice.Add(args.NewOrder.LastFillPrice);
                fillVolume.Add(args.NewOrder.LastFillVolume);
                Print($"Volume: {args.NewOrder.LastFillVolume}, price: {args.NewOrder.LastFillPrice}");
                if (args.NewOrder.RemainingVolume == 0)
                    OnStop();
            }
        }

        private double CalcAveragePrice()
        {
            var avgPrice = 0.0;
            for (var i = 0; i < fillPrice.Count; i++)
            {
                avgPrice += fillVolume[i] * fillPrice[i];
                avgPrice /= Volume;
            }
            return avgPrice;
        }

        private double GetFilledVolume()
        {
            var filledVolume = 0.0;
            for (var i = 0; i < fillPrice.Count; i++)
            {
                filledVolume += fillVolume[i];
            }
            return filledVolume;
        }

        protected override async Task AsyncStop()
        {
            try
            {
                if (Account.Orders[limitOrderId] != null)
                    await CancelOrderAsync(limitOrderId);
            }
            catch (Exception e)
            {
                Print(e.Message);
            }
            Status.WriteLine($"Average price of {Side.ToString().ToLower()} order: {CalcAveragePrice()}, filled volume: {GetFilledVolume()}");
        }
    }
}
