using System;
using System.Collections.Generic;
using System.Linq;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace TPtoAllNewPositionsInPercents
{
    internal sealed class TradePair
    {
        private const double MinCoef = 0.9999;

        private readonly TPtoAllNewPositionsInPercents _bot;

        private readonly double _tpPercent, _minLimitVolume;
        private readonly string _orderComment;
        private readonly string _symbol;

        private List<Order> OrdersChain => _bot.Account.OrdersBySymbol(_symbol).Where(u => u.Comment.StartsWith(_bot.CommentPrefix)).ToList();

        private NetPosition Position => _bot.Account.NetPositions.FirstOrDefault(u => u.Symbol == _symbol);


        public TradePair(TPtoAllNewPositionsInPercents bot, string symbol)
        {
            _bot = bot;
            _symbol = symbol;

            _tpPercent = _bot.Config.TryGetTP(symbol);
            _minLimitVolume = _bot.Config.TryGetMinVolume(symbol);

            _orderComment = $"{_bot.CommentPrefix}{_tpPercent}";
        }


        public void RecalculateChain()
        {
            var symbol = _bot.Symbols[_symbol];

            if (!symbol.IsTradeAllowed)
                return;

            var minVolume = Math.Max(symbol.MinTradeVolume, _minLimitVolume);

            FilterChain(minVolume);

            var position = Position;

            if (position == null)
                return;

            var newLimitPrice = GetNewLimitPrice(out var openedLimitsVolume);
            var newLimitVolume = position.Volume - openedLimitsVolume;

            if (newLimitVolume.Gt(0.0))
            {
                while (newLimitVolume.Gte(minVolume)) //High Volume Protection (more than MaxTradeVolume)
                {
                    var volume = Math.Min(symbol.MaxTradeVolume, newLimitVolume);

                    OpenLimitOrder(newLimitPrice, volume);
                    newLimitVolume -= volume;
                }
            }
            else if (newLimitVolume.Lt(0.0))
                ReduceTotalLimitVolume(openedLimitsVolume);
        }

        public void RemoveChain() => OrdersChain.ForEach(o => _bot.CancelOrder(o.Id));


        private void OpenLimitOrder(double price, double volume)
        {
            var side = Position.Side.Inversed();
            var status = _bot.OpenOrder(_symbol, OrderType.Limit, side, volume, null, price, null, comment: _orderComment);

            if (status.ResultCode == OrderCmdResultCodes.Ok)
                _bot.Print($"Symbol={_symbol}, TpSymbol={price}");
        }

        private void ReduceTotalLimitVolume(double openedLimitsVolume)
        {
            foreach (var limit in OrdersChain.OrderBy(u => u.RemainingVolume))
                if (Position.Volume.Lt(openedLimitsVolume))
                {
                    openedLimitsVolume -= limit.RemainingVolume;
                    _bot.CancelOrder(limit.Id);
                }

            RecalculateChain();
        }

        private void FilterChain(double minVolume) //Protection against side changes and old tp settings
        {
            var position = Position;

            foreach (var order in OrdersChain)
                if (position == null || position.Side == order.Side || order.Comment != _orderComment || order.RemainingVolume.Lt(minVolume))
                    _bot.CancelOrder(order.Id);
        }

        private double GetNewLimitPrice(out double limitVolume)
        {
            var limitPrice = GetVWAPLimitPrice(out limitVolume);
            var position = Position;

            return (position.Volume * GetNewTPPrice(position.Price) - limitVolume * limitPrice) / (position.Volume - limitVolume);
        }

        private double GetVWAPLimitPrice(out double limitVolume)
        {
            var ans = 0.0;
            limitVolume = 0.0;

            foreach (var limit in OrdersChain)
            {
                ans += limit.Price * limit.RemainingVolume;
                limitVolume += limit.RemainingVolume;
            }

            return limitVolume.E(0.0) ? 0.0 : ans / limitVolume;
        }

        private double GetNewTPPrice(double price)
        {
            return price * (Position.Side.IsSell() ? 1 - Math.Min(_tpPercent, MinCoef) : 1 + _tpPercent);
        }
    }
}
