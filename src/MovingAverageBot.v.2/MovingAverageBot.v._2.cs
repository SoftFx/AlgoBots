﻿using SoftFx;
using System;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Indicators;
using TickTrader.Algo.Api.Math;

namespace MovingAverageBotv2
{
    [TradeBot(DisplayName = "MovingAverageBot.v2", Category = CommonConstants.Category, Version = "1.0")]
    public class MovingAverageBot : TradeBot
    {
        [Parameter(DisplayName = "MaximumRisk", DefaultValue = 0.02)]
        public double MaximumRisk { get; set; }

        [Parameter(DisplayName = "DecreaseFactor", DefaultValue = 3)]
        public double DecreaseFactor { get; set; }

        [Parameter(DisplayName = "MovingPeriod", DefaultValue = 12)]
        public int MovingPeriod { get; set; }

        [Parameter(DisplayName = "MovingShift", DefaultValue = 6)]
        public int MovingShift { get; set; }

        [Parameter(DefaultValue = 1000)]
        public int Margin { get; set; }

        private IMovingAverage _iMA;
        private int _losses = 0;
        private DateTime _openTimeLastBar = DateTime.MinValue;

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

            OutputParametrsOnStatus();
            OutputParametrsOnLog();
            _iMA = Indicators.MovingAverage(Bars.Close, MovingPeriod, MovingShift);
        }

        protected override void OnQuote(Quote quote)
        {
            //if (Bars[0].OpenTime <= _openTimeLastBar)
            //    return;

            _openTimeLastBar = Bars[0].OpenTime;

            if (GetCountCurrentOrders() == 0)
                CheckForOpen();
            else
                CheckForClose();
        }

        private int GetCountCurrentOrders()
        {
            return Account.Type == AccountTypes.Gross ? Account.Orders.Count : CalculateNetPositions();
        }

        private int CalculateNetPositions()
        {
            foreach (NetPosition position in Account.NetPositions)
                if (position.Symbol == Symbol.Name)
                    return 1;
            return 0;
        }

        private double LotsOptimized(OrderSide side)
        {
            var symbol = side == OrderSide.Buy ? Symbol.Ask : Symbol.Bid;

            string message = $"";

            message += $"Price for 1 volume = {Margin:F8} \n";
            message += $"Account margin = {Account.Margin}, balance = {Account.Balance}, account free margin = {Account.Balance - Account.Margin} \n";

            double lot = ((Account.Balance - Account.Margin) * MaximumRisk / Margin).Round(Symbol.TradeVolumeStep);

            message += $"Maximum calculated volume = {lot:F8} \n";

            if (DecreaseFactor > 0)
            {
                for (int i = 2; i <= _losses; ++i)
                    lot = (lot - lot * i / DecreaseFactor).Round(Symbol.TradeVolumeStep);
            }

            message += $"Lose streak: {_losses} \n";
            message += $"Calculated possible volume = {lot:F7}, max trade volume = {Symbol.MaxTradeVolume:F7}, min trade volume = {Symbol.MinTradeVolume:F12}";

            Print(message);

            if (lot > Symbol.MaxTradeVolume)
                lot = Symbol.MaxTradeVolume;

            return lot < Symbol.MinTradeVolume ? Symbol.MinTradeVolume : lot;
        }

        private void CheckForOpen()
        {
            if (Bars[0].Volume > 1)
                return;

            double ma = _iMA.Average[_iMA.LastPositionChanged];

            Print($"Work {ma}"); //for test

            if (Bars.Open[1] > ma && Bars.Close[1] < ma)
            {
                Print($"Open: {Bars.Open[1]} Close: {Bars.Close[1]} Bid: {Bid} Ask: {Ask}");
                double openVolume = LotsOptimized(OrderSide.Sell);
                if (openVolume != double.NaN && OpenOrder(symbol: Symbol.Name, type: OrderType.Market, side: OrderSide.Sell, volume: openVolume, price: Bid, maxVisibleVolume: null, stopPrice: null).ResultCode == OrderCmdResultCodes.Ok)
                    Print($"Open volume = {openVolume}, price = {Bid}");
                return;
            }

            if (Bars.Open[1] < ma && Bars.Close[1] > ma)
            {
                Print($"Open: {Bars.Open[1]} Close: {Bars.Close[1]} Ask: {Ask} Bid: {Bid}");
                double openVolume = LotsOptimized(OrderSide.Buy);
                if (openVolume != double.NaN && OpenOrder(symbol: Symbol.Name, type: OrderType.Market, side: OrderSide.Buy, volume: openVolume, price: Ask, maxVisibleVolume: null, stopPrice: null).ResultCode == OrderCmdResultCodes.Ok)
                    Print($"Open volume = {openVolume}, price = {Ask}");
                return;
            }
        }

        private void CheckForClose()
        {
            if (Bars[0].Volume > 1)
                return;

            double ma = _iMA.Average[0];

            Print($"Work {ma}"); //for test

            if (Account.Type == AccountTypes.Gross)
                CheckCloseForGross(ma);
            else
                CheckCloseForNet(ma);
        }

        private void CheckCloseForNet(double ma)
        {
            foreach (NetPosition position in Account.NetPositions)
            {
                if (position.Side == OrderSide.Buy && position.Symbol == Symbol.Name)
                {
                    if (Bars.Open[1] > ma && Bars.Close[1] < ma)
                        CloseCurrentOrderForNet(position);
                    break;
                }

                if (position.Side == OrderSide.Sell && position.Symbol == Symbol.Name)
                {
                    if (Bars.Open[1] < ma && Bars.Close[1] > ma)
                        CloseCurrentOrderForNet(position);
                    break;
                }
            }
        }

        private void CheckCloseForGross(double ma)
        {
            foreach (Order order in Account.Orders)
            {
                if (order.Side == OrderSide.Buy && order.Symbol == Symbol.Name)
                {
                    if (Bars.Open[1] > ma && Bars.Close[1] < ma)
                        CloseCurrentOrderForGross(order);
                    break;
                }

                if (order.Side == OrderSide.Sell && order.Symbol == Symbol.Name)
                {
                    if (Bars.Open[1] < ma && Bars.Close[1] > ma)
                        CloseCurrentOrderForGross(order);
                    break;
                }
            }
        }

        private void CloseCurrentOrderForNet(NetPosition position)
        {
            CalculateCurrentLoseStreak(position.Profit);

            var side = position.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
            var price = side == OrderSide.Buy ? Ask : Bid;
            if (OpenOrder(symbol: Symbol.Name, type: OrderType.Market, side: side, volume: position.Volume, price: price, maxVisibleVolume: null, stopPrice: null).ResultCode == OrderCmdResultCodes.Ok)
                Print($"Close volume = {position.Volume}, price = {price}");
        }

        private void CloseCurrentOrderForGross(Order order)
        {
            CalculateCurrentLoseStreak(order.Profit);
            if (CloseOrder(order.Id, order.RemainingVolume).ResultCode == OrderCmdResultCodes.Ok)
                Print($"Close volume = {order.RemainingVolume}, price = {order.Price}");
        }

        private void CalculateCurrentLoseStreak(double profit)
        {
            _losses = profit > 0 ? 0 : profit == 0 ? _losses : ++_losses;
            OutputParametrsOnStatus();
        }

        private void OutputParametrsOnStatus()
        {
            Status.WriteLine($"Maximum Risk: {MaximumRisk:F4}");
            Status.WriteLine($"Decrease Factor: {DecreaseFactor:F4}");
            Status.WriteLine($"Moving Period: {MovingPeriod}");
            Status.WriteLine($"Moving Shift: {MovingShift}");
            Status.WriteLine($"Lose streak: {_losses}");
        }

        private void OutputParametrsOnLog()
        {
            string message = $"";
            message += $"Maximum Risk: {MaximumRisk:F4} \n";
            message += $"Decrease Factor: {DecreaseFactor:F4} \n";
            message += $"Moving Period: {MovingPeriod} \n";
            message += $"Moving Shift: {MovingShift}";
            Print(message);
        }

        private void Validations()
        {
            if (Account.Type != AccountTypes.Gross && Account.Type != AccountTypes.Net)
                throw new ValidationException("Account type is not Gross or NET");

            if (MaximumRisk <= 0)
                throw new ValidationException("Maximum risk must be more than 0");

            if (MaximumRisk > 1)
                throw new ValidationException("Maximum rist must be less than or equal 1");

            if (DecreaseFactor <= 0)
                throw new ValidationException("Decrease Factor must be more than 0");

            if (MovingPeriod <= 0)
                throw new ValidationException("Moving Period less than than 0");
        }
    }
}