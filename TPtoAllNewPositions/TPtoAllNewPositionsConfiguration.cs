using SoftFx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TPtoAllNewPositions
{
    public class TPtoAllNewPositionsConfiguration : BotConfig
    {
        [Nett.TomlIgnore]
        public HashSet<string> ExcludeSymbolsHash { get; } = new();

        [Nett.TomlIgnore]
        public Dictionary<string, int> SymbolsTP { get; } = new();


        public Dictionary<string, string> SymbolsSettings { get; set; }

        public List<string> ExcludeSymbols { get; set; }


        public int RunIntervalInSeconds { get; set; }

        public int DefaultTPInPips { get; set; }

        public int TpForCurrentPriceInPips { get; set; }


        public TPtoAllNewPositionsConfiguration()
        {
            RunIntervalInSeconds = 600;
            DefaultTPInPips = 100;
            TpForCurrentPriceInPips = 5;

            SymbolsSettings = new Dictionary<string, string>()
            {
                ["AUDCAD"] = "200",
                ["AUDCHF"] = "200",
                ["USDMXN"] = "3000",
                ["USDRUB"] = "1000",
            };

            ExcludeSymbols = new List<string>() { "BTCUSD" };
        }


        public override void Init()
        {
            if (RunIntervalInSeconds <= 0)
                throw new ValidationException($"{nameof(RunIntervalInSeconds)} must be greater than 0");

            if (DefaultTPInPips < 0)
                throw new ValidationException($"{nameof(DefaultTPInPips)} must be greater or equal than 0");

            if (TpForCurrentPriceInPips < 0)
                throw new ValidationException($"{nameof(TpForCurrentPriceInPips)} must be greater or equal than 0");

            foreach ((var symbol, var tp) in SymbolsSettings)
                if (int.TryParse(tp, out var pips))
                    PrintError($"{symbol} invalid tp = {tp} (cannot be parsed to int)");
                else
                    SymbolsTP.TryAdd(symbol, pips);

            ExcludeSymbolsHash.Clear();

            foreach (var symbol in ExcludeSymbols)
                ExcludeSymbolsHash.Add(symbol);
        }


        public bool TryGetTP(string symbol, out double tp)
        {
            tp = SymbolsTP.TryGetValue(symbol, out var pips) ? pips : DefaultTPInPips;

            return ExcludeSymbolsHash.Contains(symbol);
        }

        public override string ToString()
        {
            var builder = new StringBuilder(1 << 10);

            builder.AppendLine("Input config:")
                   .AppendLine($"{nameof(RunIntervalInSeconds)} = {RunIntervalInSeconds};")
                   .AppendLine($"{nameof(DefaultTPInPips)} = {DefaultTPInPips};")
                   .AppendLine($"{nameof(TpForCurrentPriceInPips)} = {TpForCurrentPriceInPips};")
                   .AppendLine()
                   .AppendLine($"[{nameof(SymbolsSettings)}]")
                   .Append($"{string.Join($"{Environment.NewLine}", SymbolsTP.Select(u => $"{u.Key}={u.Value}"))}")
                   .AppendLine()
                   .AppendLine($"[{nameof(ExcludeSymbols)}]")
                   .Append($"{string.Join(",", ExcludeSymbols)}");

            return builder.ToString();
        }
    }
}
