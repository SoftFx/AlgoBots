﻿using System.Collections.Generic;
using System.Linq;
using TickTrader.Algo.Api;

namespace TPtoAllNewPositionsInPercents
{
    internal sealed class LimitWatcher
    {
        private readonly Dictionary<string, TradePair> _pairs = new Dictionary<string, TradePair>();
        private readonly TPtoAllNewPositionsInPercents _bot;


        public LimitWatcher(TPtoAllNewPositionsInPercents bot)
        {
            _bot = bot;

            UploadPosition();
            CloseOldPositions(); //Closing a chain with a closed position
        }


        public void UploadPosition()
        {
            _bot.Account.NetPositions.ToList().ForEach(AddTradePair); //adding chains for new positions
            _pairs.Values.ToList().ForEach(u => u.RecalculateChain());
        }

        public void UploadPosition(NetPositionModifiedEventArgs obj)
        {
            if (obj.IsClosed)
                RemoveTradePair(obj.OldPosition);
        }

        private void AddTradePair(NetPosition position)
        {
            var symbol = position.Symbol;

            if (!_pairs.ContainsKey(symbol))
                _pairs.Add(symbol, new TradePair(_bot, symbol));
        }

        private void RemoveTradePair(NetPosition position)
        {
            var symbol = position.Symbol;

            if (_pairs.TryGetValue(symbol, out TradePair pair))
            {
                _pairs.Remove(symbol);
                pair.RemoveChain();
            }
        }

        private void CloseOldPositions() => _bot.Account.Orders.Where(u => u.Comment.StartsWith(_bot.CommentPrefix) && !_pairs.ContainsKey(u.Symbol)).ToList()
                                                               .ForEach(u => _bot.CancelOrder(u.Id));
    }
}
