using _100YearPortfolio.Portfolio;
using System.Globalization;
using static _100YearPortfolio.Portfolio.PortfolioConfig;

namespace _100YearPortfolio
{
    internal sealed class PortfolioReader
    {
        private const string SymbolNameHeader = "Symbol";
        private const string PercentNameHeader = "Distribution";
        private const string MaxLotsNameHeader = "MaxLotsSum";

        private readonly List<string> _expectedSettings = new()
        {
            UpdateMinSettingName,
            BalanceTypeSettingName,
            EquityMinLevelSettingName,
            EquityUpdateTimeName,
            DefaultMaxLotSizeSettingName,
            StatusUpdateTimeoutName
        };


        public bool TryReadSettings(List<List<string>> configStr, out PortfolioConfig config, out string error)
        {
            static string GetSettingReadError(string setting, string val) => $"Invalid format {setting} = {val}";

            error = null;
            config = null;

            var updateMin = 1;
            var updateStatusSec = 1;
            var minEquityLevel = 0.0;
            var equituUpdateTime = 0;
            var defaultMaxLotSize = 0.0;
            var balanceType = BalanceTypeEnum.Balance;

            foreach (var item in configStr)
                if (item.Count > 1 && _expectedSettings.Contains(item[0]))
                {
                    var settingName = item[0];
                    var valueStr = item[1];

                    var ok = settingName switch
                    {
                        UpdateMinSettingName => int.TryParse(valueStr, out updateMin),
                        StatusUpdateTimeoutName => int.TryParse(valueStr, out updateStatusSec),
                        BalanceTypeSettingName => Enum.TryParse(valueStr, true, out balanceType),
                        EquityMinLevelSettingName => TryReadPercent(valueStr, out minEquityLevel),
                        EquityUpdateTimeName => int.TryParse(valueStr, out equituUpdateTime),
                        DefaultMaxLotSizeSettingName => TryParseInvariantDouble(valueStr, out defaultMaxLotSize),
                    };

                    if (!ok)
                        error = GetSettingReadError(settingName, valueStr);

                    _expectedSettings.Remove(settingName);
                }

            if (_expectedSettings.Count == 0)
            {
                config = new PortfolioConfig
                {
                    UpdateMinutes = updateMin,
                    StatusUpdateTimeoutSec = updateStatusSec,
                    BalanceType = balanceType,
                    EquityMinLevel = minEquityLevel,
                    EquityUpdateTime = equituUpdateTime,
                    DefaultMaxLotSize = defaultMaxLotSize,
                };
            }
            else
                error = $"Some settings not found: {string.Join(',', _expectedSettings)}";

            return string.IsNullOrEmpty(error);
        }


        public static bool TryReadPortfolio(PortfolioBot bot, List<List<string>> portfolioStr, out MarketState marketState, out string error)
        {
            marketState = new MarketState();
            error = null;

            var lineNumber = 0;

            // Skip header if it exists
            if (portfolioStr?.FirstOrDefault()?.FirstOrDefault()?.StartsWith(SymbolNameHeader) ?? false)
                lineNumber++;

            try
            {
                for (; lineNumber < portfolioStr.Count; ++lineNumber)
                {
                    var line = portfolioStr[lineNumber];

                    if (line.Count < 1)
                        throw new Exception($"Invalid line format.");

                    var symbolName = line[0];
                    var maxSumLot = (double?)null;
                    var percent = 0.0;

                    if (line.Count > 1 && !string.IsNullOrEmpty(line[1]))
                    {
                        var percentStr = line[1];

                        if (!TryReadPercent(percentStr, out percent))
                            throw new Exception($"Incorrect {PercentNameHeader} = {percentStr}.");
                    }

                    if (line.Count > 2 && !string.IsNullOrEmpty(line[2]))
                    {
                        var maxSumLotStr = line[2];

                        if (!TryParseInvariantDouble(maxSumLotStr, out var maxSumLotDouble))
                            throw new Exception($"Incorrect {MaxLotsNameHeader} = {maxSumLotStr}.");
                        else
                            maxSumLot = maxSumLotDouble;
                    }

                    var symbol = new MarketSymbol(bot, symbolName, percent, maxSumLot);

                    if (!marketState.AddSymbol(symbol))
                        throw new Exception($"Symbol {symbolName} is duplicated.");
                }

                if (!marketState.CheckTotalPercent())
                    throw new Exception("Percentage is greater than 100%");

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;

                if (lineNumber < portfolioStr.Count)
                    error += $" Line #{lineNumber + 1}";
            }

            return string.IsNullOrEmpty(error);
        }

        private static bool TryReadPercent(string str, out double percent)
        {
            return TryParseInvariantDouble(str.TrimEnd('%'), out percent);
        }

        private static bool TryParseInvariantDouble(string str, out double val)
        {
            return double.TryParse(str.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out val);
        }
    }
}