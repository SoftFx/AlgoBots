using TickTrader.Algo.Api;

namespace SoftFx.Common.Adapters
{
    public class BookEntryAdapter
    {
        public double Price { get; }

        public double Volume { get; set; }


        public BookEntryAdapter(double price, double volume)
        {
            Price = price;
            Volume = volume;
        }

        public BookEntryAdapter(BookEntry src)
        {
            Price = src.Price;
            Volume = src.Volume;
        }


        public override string ToString()
        {
            return $"{Volume} at {Price}";
        }
    }
}
