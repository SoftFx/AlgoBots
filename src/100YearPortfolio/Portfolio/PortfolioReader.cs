using System.Globalization;
using TickTrader.Algo.Api.Math;
using TradeFile = TickTrader.Algo.Api.File;

namespace _100YearPortfolio
{
    internal sealed class PortfolioReader
    {
        private readonly PortfolioBot _bot;


        public PortfolioReader(PortfolioBot bot)
        {
            _bot = bot;
        }


        public bool TryRead(TradeFile file, out MarketState marketState)
        {
            marketState = new MarketState();

            try
            {
                using var fileStream = file.Open(FileMode.Open);
                using var reader = new StreamReader(fileStream);

                var lineNumber = 0;

                while (!reader.EndOfStream)
                {
                    var parts = reader.ReadLine().Split(_bot.Separator);
                    lineNumber++;

                    if (parts.Length < 2)
                        throw new Exception($"Invalid line format line #{lineNumber}");

                    if (!TryReadPercent(parts[1], out var percent))
                        throw new Exception($"Incorrect double value {parts[1]} line #{lineNumber}");

                    var symbolName = parts[0];
                    var symbol = new MarketSymbol(_bot, symbolName, percent);

                    marketState.AddSymbol(symbol);
                }

                if (!marketState.CheckTotalPercent())
                    throw new Exception("Percentage is greater than 100%");

                return true;
            }
            catch (Exception ex)
            {
                _bot.PrintError(ex.Message);
                return false;
            }
        }

        private static bool TryReadPercent(string str, out double percent)
        {
            return double.TryParse(str.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out percent);
        }
    }
}