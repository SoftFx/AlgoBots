using System;
using System.Collections.Generic;
using TickTrader.Algo.Api;

namespace SoftFx.SmartTradeBot
{
    [TradeBot(DisplayName = "SmartTradeBot", Category = CommonConstants.Category, Version = "1.1")]
    public class SmartTradeBot : TradeBot
    {
        private double _price;
        private string _limitOrderId;
        private List<double> _fillVolume = new List<double>();
        private List<double> _fillPrice = new List<double>();


        [Parameter(DisplayName = "Volume", DefaultValue = 1)]
        public double Volume { get; set; }

        [Parameter(DisplayName = "Order side", DefaultValue = OrderSide.Buy)]
        public OrderSide Side { get; set; }

        protected override void Init()
        {
            Feed.Subscribe(Symbol.Name, 2);
            _price = (Side == OrderSide.Buy) ? Symbol.Bid : Symbol.Ask;
            Account.Orders.Filled += OrderFilledEvent;
            _limitOrderId = OpenOrder(Symbol.Name, OrderType.Limit, Side, Volume, _price).ResultingOrder.Id;
        }

        protected override void OnQuote(Quote quote)
        {
            _price = GetCurrentPrice(quote);
            var limitOrder = Account.Orders[_limitOrderId];

            if (!limitOrder.IsNull && _price != limitOrder.Price)
                ModifyOrder(_limitOrderId, _price);
        }

        private BookEntry[] GetBookEntry(Quote quote)
        {
            return (Side == OrderSide.Buy) ? quote.BidBook : quote.AskBook;
        }

        private double GetCurrentPrice(Quote quote)
        {
            BookEntry[] book = GetBookEntry(quote);

            if (_price == book[0].Price && Volume == book[0].Volume)
                return book[1].Price;
            else
                return book[0].Price;
        }

        private void OrderFilledEvent(OrderFilledEventArgs args)
        {

            if (args.OldOrder.Id == _limitOrderId)
            {
                _fillPrice.Add(args.NewOrder.LastFillPrice);
                _fillVolume.Add(args.NewOrder.LastFillVolume);
                Print($"Volume: {args.NewOrder.LastFillVolume}, price: {args.NewOrder.LastFillPrice}");
                if (args.NewOrder.RemainingVolume == 0)
                    Exit();
            }
        }

        private double CalcAveragePrice()
        {
            var avgPrice = 0.0;
            for (var i = 0; i < _fillPrice.Count; i++)
            {
                avgPrice += _fillVolume[i] * _fillPrice[i];
                avgPrice /= Volume;
            }
            return avgPrice;
        }

        private double GetFilledVolume()
        {
            var filledVolume = 0.0;
            for (var i = 0; i < _fillPrice.Count; i++)
            {
                filledVolume += _fillVolume[i];
            }
            return filledVolume;
        }

        protected override void OnStop()
        {
            try
            {
                if (!Account.Orders[_limitOrderId].IsNull)
                    CancelOrder(_limitOrderId);
            }
            catch (Exception e)
            {
                Print(e.Message);
            }
            Status.WriteLine($"Average price of {Side.ToString().ToLower()} order: {CalcAveragePrice()}, filled volume: {GetFilledVolume()}");
        }
    }
}
