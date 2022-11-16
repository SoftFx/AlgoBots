using SoftFx;
using System;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Indicators;

namespace MACDsampleBot
{
    [TradeBot(DisplayName = "MACDsampleBot", Category = "SoftFX Public", Version = "1.1")]
    public class MACDsampleBot : TradeBot
    {
        [Parameter(DisplayName = "TakeProfit", DefaultValue = 50)]
        public double TakeProfit { get; set; }

        [Parameter(DisplayName = "Lots", DefaultValue = 0.1)]
        public double Lots { get; set; }

        [Parameter(DisplayName = "TralingStop", DefaultValue = 30)]
        public double TralingStop { get; set; }

        [Parameter(DisplayName = "MACDOpenLevel", DefaultValue = 3)]
        public double MACDOpenLevel { get; set; }

        [Parameter(DisplayName = "MACDCloseLevel", DefaultValue = 2)]
        public double MACDCloseLevel { get; set; }

        [Parameter(DisplayName = "MATrendPeriod", DefaultValue = 26)]
        public int MATrendPeriod { get; set; }

        private IMovingAverage _iMA;
        private IMacd _iMacd;
        private double _macdCurrent, _macdPrevious, _signalCurrent, _signalPrevious, _maCurrent, _maPrevious;


        protected override void Init()
        {
            try
            {
                Validations();
            }
            catch (Exception ex)
            {
                Status.WriteLine(ex.Message);
                PrintError(ex.Message);
                Exit();
                return;
            }

            _iMacd = Indicators.MACD(Bars.Close, 12, 26, 9);

            _iMA = Indicators.MovingAverage(Bars.Close, MATrendPeriod, 0, MovingAverageMethod.Exponential);

            OutputParametrs();
        }

        protected override void OnQuote(Quote quote)
        {
            //if (Bars.Count < 100)
            //{
            //	PrintError("Bars less than 100");
            //	return;
            //}

            _macdCurrent = _iMacd.MacdSeries[0];
            _macdPrevious = _iMacd.MacdSeries[1];
            _signalCurrent = _iMacd.Signal[0];
            _signalPrevious = _iMacd.Signal[1];
            _maCurrent = _iMA.Average[0];
            _maPrevious = _iMA.Average[1];

            //Print($"MA = {_maCurrent}, Close = {Bars[0].Close}");

            if (Account.Orders.Count < 1)
            {
                if (HaveCurrentFreeMargin(OrderSide.Buy))
                {
                    if (_macdCurrent < 0 && (_macdCurrent > _signalCurrent) && (_macdPrevious < _signalPrevious) && (Math.Abs(_macdCurrent) > (MACDOpenLevel * Symbol.Point)) && (_maCurrent > _maPrevious))
                    {
                        OpenOrder(symbol: Symbol.Name, type: OrderType.Market, side: OrderSide.Buy, volume: Lots, price: Ask + 3 * Symbol.Point, sl: null, tp: Ask + TakeProfit * Symbol.Point, maxVisibleVolume: null, stopPrice: null);
                        return;
                    }
                }

                if (HaveCurrentFreeMargin(OrderSide.Sell))
                {
                    if (_macdCurrent > 0 && (_macdCurrent < _signalCurrent) && (_macdPrevious > _signalPrevious) && (_macdCurrent > (MACDOpenLevel * Symbol.Point)) && (_maCurrent < _maPrevious))
                    {
                        OpenOrder(symbol: Symbol.Name, type: OrderType.Market, side: OrderSide.Sell, volume: Lots, price: Bid - 3 * Symbol.Point, sl: null, tp: Bid - TakeProfit * Symbol.Point, maxVisibleVolume: null, stopPrice: null);
                        return;
                    }
                }
            }

            foreach (var order in Account.Orders)
            {
                if (order.Side == OrderSide.Buy)
                {
                    if (_macdCurrent > 0 && (_macdCurrent < _signalCurrent) && (_macdPrevious > _signalPrevious) && (_macdCurrent > (MACDCloseLevel * Symbol.Point)))
                    {
                        CloseOrder(order.Id, order.RemainingVolume);
                        return;
                    }

                    if (Bid - order.Price > Symbol.Point * TralingStop)
                        if (order.StopLoss < Bid - Symbol.Point * TralingStop)
                        {
                            ModifyOrder(order.Id, order.Price, null, sl: Bid - Symbol.Point * TralingStop, tp: order.TakeProfit);
                            return;
                        }
                }
                else
                {
                    if (_macdCurrent < 0 && (_macdCurrent > _signalCurrent) && (_macdPrevious < _signalPrevious) && (Math.Abs(_macdCurrent) > (MACDCloseLevel * Symbol.Point)))
                    {
                        CloseOrder(order.Id, order.RemainingVolume);
                        return;
                    }

                    if (order.Price - Ask > Symbol.Point * TralingStop)
                        if ((order.StopLoss > Ask + Symbol.Point * TralingStop) || (order.StopLoss == 0))
                        {
                            ModifyOrder(order.Id, order.Price, null, sl: Ask + Symbol.Point * TralingStop, tp: order.TakeProfit);
                            return;
                        }
                }
            }

        }

        private double? GetCurrentMargin(OrderSide side)
        {
            var symbol = side == OrderSide.Buy ? Symbol.Ask : Symbol.Bid;
            return Account.CalculateOrderMargin(Symbol.Name, OrderType.Market, side, 1.0, null, symbol, null);
        }

        private bool HaveCurrentFreeMargin(OrderSide side)
        {
            var margin = GetCurrentMargin(side);

            return margin.HasValue && (Account.Balance - Account.Margin) > margin * Lots;
        }

        private void OutputParametrs()
        {
            Status.WriteLine($"Take Profit: {TakeProfit}");
            Status.WriteLine($"Lots: {Lots:F6}");
            Status.WriteLine($"Traling Stop: {TralingStop}");
            Status.WriteLine($"Macd Open Level: {MACDCloseLevel}");
            Status.WriteLine($"Macd Close Level: {MACDCloseLevel}");
            Status.WriteLine($"MA trend period: {MATrendPeriod}");
        }

        private void Validations()
        {
            if (Account.Type != AccountTypes.Gross)
                throw new ValidationException("Account type is not Gross");

            if (TakeProfit < 10)
                throw new ValidationException("Take Profit less than 10");

            if (Lots < Symbol.MinTradeVolume)
                throw new ValidationException($"Lots less than Min Trade Volume {Symbol.MinTradeVolume}");

            if (TralingStop < 0)
                throw new ValidationException("Traling stop less than 0");

            if (MACDOpenLevel < 0)
                throw new ValidationException("Macd Open Level less than 0");

            if (MACDCloseLevel < 0)
                throw new ValidationException("Macd Close Level less than 0");

            if (MATrendPeriod <= 0)
                throw new ValidationException("MA trend period less than 0");
        }
    }
}