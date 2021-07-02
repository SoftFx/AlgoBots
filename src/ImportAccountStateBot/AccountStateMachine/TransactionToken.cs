using SoftFx.Common.Extensions;
using System;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace ImportAccountStateBot
{
    public sealed class TransactionToken
    {
        public string Symbol { get; }

        public double Volume { get; private set; }

        public OrderSide Side { get; private set; }

        public bool IsEmpty => Volume.E(0.0);


        private TransactionToken(string symbol, double volume, OrderSide side)
        {
            Symbol = symbol;
            Volume = volume;
            Side = side;
        }

        private TransactionToken(TransactionToken token) : this(token.Symbol, token.Volume, token.Side) { }

        public TransactionToken(string symbol) : this(symbol, 0.0, OrderSide.Buy) { }

        public TransactionToken(Order order) : this(order.Symbol, order.RemainingVolume, order.Side) { }

        public TransactionToken(PositionState pos) : this(pos.Symbol, pos.Volume, pos.Side) { }


        public static TransactionToken operator +(TransactionToken first, TransactionToken second)
        {
            if (first.Symbol != second.Symbol)
                return null;

            return new TransactionToken(first)
            {
                Side = first.Volume > second.Volume ? first.Side : second.Side,
                Volume = first.Side == second.Side ? first.Volume + second.Volume :
                                                     Math.Abs(first.Volume - second.Volume),
            };
        }

        public static TransactionToken operator -(TransactionToken first, TransactionToken second)
        {
            if (first.Symbol != second.Symbol || first.Side != second.Side)
                return null;

            return new TransactionToken(first)
            {
                Volume = Math.Abs(first.Volume - second.Volume)
            };
        }

        public static TransactionToken operator ~(TransactionToken token)
        {
            return new TransactionToken(token)
            {
                Side = token.Side.Inverse()
            };
        }

        public static TransactionToken Min(double volume, TransactionToken token)
        {
            if (token.Volume.Lte(volume))
                return token;

            return new TransactionToken(token)
            {
                Volume = volume,
            };
        }


        public override string ToString()
        {
            return $"Token {Symbol} {Side} {Volume}";
        }
    }
}
