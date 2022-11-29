using _100YearPortfolio.Portfolio;
using System.Globalization;
using static _100YearPortfolio.Portfolio.PortfolioConfig;

namespace _100YearPortfolio
{
    internal sealed class PortfolioReader
    {
        private const string SymbolNameHeader = "Symbol";
        private const string PercentNameHeader = "Distribution";
        private const string MaxLotSizeHeader = "MaxLotSize";
        private const string OriginSymbolHeader = "Symbol";

        private readonly static char[] _noteSeparators = new char[] { '\n', ';' };

        private readonly List<string> _expectedSettings = new()
        {
            UpdateMinSettingName,
            BalanceTypeSettingName,
            EquityMinLevelSettingName,
            EquityUpdateTimeName,
        };

        private readonly List<string> _optionalSettings = new()
        {
            StatusUpdateTimeoutName
        };


        public bool TryReadConfig(List<List<string>> configStr, out PortfolioConfig config, out string error)
        {
            static string GetSettingReadError(string setting, string val) => $"Invalid format {setting} = {val}";

            error = null;
            config = null;

            var updateMin = 1;
            var minEquityLevel = 0.0;
            var equituUpdateTime = 0;
            var balanceType = BalanceTypeEnum.Balance;

            var updateStatusSec = DefaultStatusUpdateTimeout;

            foreach (var item in configStr)
                if (item.Count > 1 && (_expectedSettings.Contains(item[0]) || _optionalSettings.Contains(item[0])))
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
                    };

                    if (!ok)
                        error = GetSettingReadError(settingName, valueStr);

                    if (_expectedSettings.Contains(settingName))
                        _expectedSettings.Remove(settingName);
                    else
                        _optionalSettings.Remove(settingName);
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
                };
            }
            else
                error = $"Some settings not found: {string.Join(',', _expectedSettings)}";

            return string.IsNullOrEmpty(error);
        }


        public static bool TryFillMarket(List<List<string>> portfolioStr, List<string> notes,
                                         MarketState market, out string error)
        {
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

                    var alias = line[0];
                    var percent = 0.0;

                    if (string.IsNullOrEmpty(alias))
                        continue;

                    if (line.Count > 1 && !string.IsNullOrEmpty(line[1]))
                    {
                        var percentStr = line[1];

                        if (!TryReadPercent(percentStr, out percent))
                            throw new Exception($"Incorrect {PercentNameHeader} = {percentStr}.");
                    }

                    var note = ParseSymbolNote(notes[lineNumber]);
                    var symbolName = note.SymbolOrigin ?? alias;

                    if (!market.AddSymbol(symbolName, alias, percent, note))
                        throw new Exception($"Symbol {symbolName} is duplicated.");
                }

                if (!market.CheckTotalPercent(out string distribution))
                    throw new Exception($"Percentage is greater than 100%: {distribution}");

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


        private static NoteSettings ParseSymbolNote(string note)
        {
            if (string.IsNullOrEmpty(note))
                return new NoteSettings();

            var rows = note.Split(_noteSeparators, StringSplitOptions.RemoveEmptyEntries);

            var symbol = (string)null;
            var maxLotSize = double.NaN;

            foreach (var row in rows)
            {
                var parts = row.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                if (parts.Count != 2)
                    throw new Exception($"Invalid note format: {row}");

                var okParseValue = parts[0] switch
                {
                    MaxLotSizeHeader => TryParseInvariantDouble(parts[1], out maxLotSize),
                    OriginSymbolHeader => CheckString(parts[1], out symbol),
                    _ => false
                };

                if (!okParseValue)
                    throw new Exception($"Invalid value for property {parts[0]} = {parts[1]}");
            }

            return new NoteSettings()
            {
                SymbolOrigin = symbol,
                MaxLotSize = double.IsNaN(maxLotSize) ? null : maxLotSize,
            };
        }


        private static bool CheckString(string str, out string result)
        {
            result = str;

            return !string.IsNullOrEmpty(result);
        }

        private static bool TryReadPercent(string str, out double percent)
        {
            str = str.Trim();
            percent = 0.0;

            if (string.IsNullOrEmpty(str))
                return false;

            var isPercentString = str[^1] == '%';
            var result = TryParseInvariantDouble(str.TrimEnd('%'), out percent);

            if (isPercentString)
                percent /= 100.0;

            return result;
        }

        private static bool TryParseInvariantDouble(string str, out double val)
        {
            return double.TryParse(str.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out val);
        }
    }
}