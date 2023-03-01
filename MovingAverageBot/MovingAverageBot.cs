﻿using SoftFx;
using System;
using System.Text;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Indicators;
using TickTrader.Algo.Api.Math;

namespace MovingAverageBot
{
    [TradeBot(DisplayName = "MovingAverageBot", Category = "SoftFX Public", Version = "1.3",
              Description = "The bot opens a trade when the price crosses the MA.")]
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

            var info = GetParamsInfo();

            Status.Write(info);
            Print(info);

            _iMA = Indicators.MovingAverage(Bars.Close, MovingPeriod, MovingShift);
        }


        protected override void OnQuote(Quote quote)
        {
            if (Bars[0].OpenTime <= _openTimeLastBar)
                return;

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

        private double LotsOptimized()
        {
            var margin = Account.CalculateOrderMargin(Symbol.Name, OrderType.Market, OrderSide.Buy, 1.0, null, Symbol.Ask, null);

            if (!margin.HasValue)
                return double.NaN;

            string message = $"";

            message += $"Margin = {margin:F8} , Contract size = {Symbol.ContractSize}, price = {Symbol.Ask} \n";
            message += $"Account margin = {Account.Margin}, balance = {Account.Balance}, account free margin = {Account.Balance - Account.Margin} \n";

            double lot = ((Account.Balance - Account.Margin) * MaximumRisk / margin.Value).Round(Symbol.TradeVolumeStep);

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
            double ma = _iMA.Average[0];

            OrderSide? side = null;

            if (Bars.Open[1] > ma && Bars.Close[1] < ma)
            {
                side = OrderSide.Sell;
            }
            else if (Bars.Open[1] < ma && Bars.Close[1] > ma)
            {
                side = OrderSide.Buy;
            }

            if (side != null)
            {
                double openVolume = LotsOptimized();
                Print($"OpenVolume: {openVolume}, MinTradeVolume: {Symbol.MinTradeVolume}");

                if (!double.IsNaN(openVolume) && OpenMarketOrder(side.Value, openVolume).ResultCode == OrderCmdResultCodes.Ok)
                    Print($"Open volume = {openVolume}, price = {(side.Value == OrderSide.Sell ? Bid : Ask)}");
            }
        }

        private void CheckForClose()
        {
            double ma = _iMA.Average[0];

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
            foreach (Order order in Account.OrdersBySymbol(Symbol.Name))
            {
                if (order.Side == OrderSide.Buy)
                {
                    if (Bars.Open[1] > ma && Bars.Close[1] < ma)
                        CloseCurrentOrderForGross(order);
                    break;
                }

                if (order.Side == OrderSide.Sell)
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

            var closeVolume = position.Volume;

            if (OpenMarketOrder(position.Side.Inversed(), closeVolume).ResultCode == OrderCmdResultCodes.Ok)
                Print($"Close volume = {closeVolume}");
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

        private string GetParamsInfo()
        {
            var sb = new StringBuilder(1 << 10);

            sb.AppendLine($"Maximum Risk: {MaximumRisk:F4}")
              .AppendLine($"Decrease Factor: {DecreaseFactor:F4}")
              .AppendLine($"Moving Period: {MovingPeriod}")
              .AppendLine($"Moving Shift: {MovingShift}")
              .AppendLine($"Lose streak: {_losses}");

            return sb.ToString();
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