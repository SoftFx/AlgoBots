using System;
using TickTrader.Algo.Api;

namespace ImportAccountStateBot
{
    public sealed class PositionState
    {
        public DateTime Time { get; }

        public double Volume { get; }

        public OrderSide Side { get; }

        public string Symbol { get; }


        private TransactionToken Token { get; }


        public PositionState(DateTime time, string symbol, OrderSide side, double volume)
        {
            Time = time;
            Volume = volume;
            Symbol = symbol;
            Side = side;

            Token = new TransactionToken(this);
        }


        public static TransactionToken GetConversionToken(PositionState current, PositionState next)
        {
            if (current == null)
                return next.Token;

            if (next == null)
                return ~current.Token;

            return ~current.Token + next.Token;
        }

        public static PositionState ParsePosition(NetPosition position, DateTime time)
        {
            return new PositionState(time, position.Symbol, position.Side, position.Volume);
        }

        public override string ToString()
        {
            return $"{Symbol} {Side} Volume={Volume}";
        }
    }
}
