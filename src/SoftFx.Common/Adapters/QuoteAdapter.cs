using System;
using System.Linq;
using TickTrader.Algo.Api;

namespace SoftFx.Common.Adapters
{
    public class QuoteAdapter : Quote
    {
        public string Symbol { get; set; }

        public DateTime Time { get; set; }

        public double Ask { get; set; }

        public double Bid { get; set; }

        public BookEntry[] AskBook { get; set; }

        public BookEntry[] BidBook { get; set; }


        public QuoteAdapter() { }

        public QuoteAdapter(Quote src)
        {
            Symbol = src.Symbol;
            Time = src.Time;
            Ask = src.Ask;
            Bid = src.Bid;
            AskBook = src.AskBook.Select(it => new BookEntryAdapter(it)).ToArray();
            BidBook = src.BidBook.Select(it => new BookEntryAdapter(it)).ToArray();
        }

        public QuoteAdapter(string symbol, Bar bidBar, Bar askBar)
        {
            if (bidBar.IsNull)
                throw new ArgumentException("Bid bar should be not null");
            if (askBar.IsNull)
                throw new ArgumentException("Ask bar should be not null");
            if (bidBar.CloseTime != askBar.CloseTime || bidBar.OpenTime != askBar.OpenTime)
                throw new ArgumentException("Bid and ask bars should be from the same time period");

            Symbol = symbol;
            Time = bidBar.CloseTime;
            Ask = askBar.Close;
            Bid = bidBar.Close;
            AskBook = new BookEntryAdapter[] { new BookEntryAdapter(askBar.Close, 0) };
            BidBook = new BookEntryAdapter[] { new BookEntryAdapter(bidBar.Close, 0) };
        }
    }
}
