using SoftFx;
using SoftFx.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TPtoAllNewPositionsInPercents
{
    public class TPtoAllNewPositionsConfiguration : BotConfig
    {
        [Nett.TomlIgnore]
        public Dictionary<string, SymbolSetting> SymbolsSettingsDict { get; } = new Dictionary<string, SymbolSetting>();

        public Dictionary<string, string> SymbolsSettings { get; set; }


        [Nett.TomlIgnore]
        public HashSet<string> ExcludedSymbolsHash { get; } = new();

        public List<string> ExcludedSymbols { get; set; }


        public int RunIntervalInSeconds { get; set; }

        public double DefaultMinVolume { get; set; }


        [Nett.TomlIgnore]
        public PriceSetting DefaultTPSettings { get; set; }

        public string DefaultTP { get; set; }


        public string TpForCurrentPrice { get; set; }

        [Nett.TomlIgnore]
        public PriceSetting TpForCurrentPriceSetting { get; set; }


        public TPtoAllNewPositionsConfiguration()
        {
            RunIntervalInSeconds = 3;
            DefaultTP = PriceSetting.DefaultTP.ToString();
            DefaultMinVolume = SymbolSetting.DefaultMinVolume;
            TpForCurrentPrice = "10";

            SymbolsSettings = new Dictionary<string, string>
            {
                {"USDRUB", "TP=0.10; MinVolume=0.1"},
                {"GBPJPY", "MinVolume=0.05; TP=0.10" },
                {"AUDUSD", "TP=0.20; MinVolume=0.1;" },
                {"EURJPY", "TP=0.10;" },
                {"AUDNZD", "TP=100p" },
                {"GBPAUD", "MinVolume=0.1; TP=0.15;" },
                {"GBPUSD", "TP=0.10; MinVolume=0.3" },
                {"NZDUSD", "TP=100pips; MinVolume = 0.22" },
                {"USDCNH", "TP=0.10; MINVOLUME=0.1" },
                {"EURCHF", "tp=0.03; minvolume=0.03" },
                {"EURUSD", "MinVolume=0.1;" },
                {"HKDJPY", "MinVolume=0.2" },
            };

            ExcludedSymbols = new List<string>();
        }

        public override void Init()
        {
            DefaultTPSettings = new PriceSetting(DefaultTP);
            TpForCurrentPriceSetting = new PriceSetting(TpForCurrentPrice);

            if (DefaultTPSettings.Value < 0.0)
                throw new ValidationException($"{nameof(DefaultTP)} must be greater or equal than 0");

            if (DefaultMinVolume <= 0.0)
                throw new ValidationException($"{nameof(DefaultMinVolume)} must be greater than 0");

            if (RunIntervalInSeconds <= 0)
                throw new ValidationException($"{nameof(RunIntervalInSeconds)} must be greater than 0");

            if (TpForCurrentPriceSetting.Value < 0.0)
                throw new ValidationException($"{nameof(TpForCurrentPriceSetting.Value)} must be greater or equal than 0");

            foreach (var pair in SymbolsSettings)
                SymbolsSettingsDict.Add(pair.Key, new SymbolSetting(pair, DefaultMinVolume));

            foreach (var symbol in ExcludedSymbols)
                ExcludedSymbolsHash.Add(symbol);

            ValidateSymbolsSettings();
        }


        public bool TryGetTP(string symbol, out PriceSetting tp)
        {
            tp = SymbolsSettingsDict.TryGetValue(symbol, out var settings) ? settings.TakeProfit : DefaultTPSettings;

            return ExcludedSymbolsHash.Contains(symbol);
        }

        public double GetMinVolume(string symbol) => SymbolsSettingsDict.TryGetValue(symbol, out var settings) ? settings.MinVolume : DefaultMinVolume;

        public bool IsExcludeSymbol(string symbol) => ExcludedSymbolsHash.Contains(symbol);


        private void ValidateSymbolsSettings()
        {
            var badPairs = new List<string>(1 << 4);

            foreach (var pair in SymbolsSettingsDict)
            {
                var error = new StringBuilder(1 << 5);

                var tp = pair.Value.TakeProfit;
                var volume = pair.Value.MinVolume;

                if (tp.Value < 0.0)
                    error.Append($"TP={tp} ");

                if (volume <= 0.0)
                    error.Append($"MinVolume={volume}");

                if (error.Length > 0)
                {
                    badPairs.Add(pair.Key);
                    PrintError($"Incorrect setting {pair.Key}: {error}");
                }
            }

            badPairs.ForEach(u => SymbolsSettingsDict.Remove(u));
        }

        public override string ToString()
        {
            var builder = new StringBuilder(1 << 10);

            builder.AppendLine("Input config:")
                   .AppendLine($"{nameof(RunIntervalInSeconds)} = {RunIntervalInSeconds};")
                   .AppendLine($"{nameof(DefaultTP)} = {DefaultTP};")
                   .AppendLine($"{nameof(DefaultMinVolume)} = {DefaultMinVolume};")
                   .AppendLine($"{nameof(TpForCurrentPrice)} = {TpForCurrentPrice};")
                   .AppendLine()
                   .AppendLine($"[{nameof(SymbolsSettings)}]")
                   .AppendLine($"{string.Join($"{Environment.NewLine}", SymbolsSettingsDict.Values.Select(u => u.ToString()))}")
                   .AppendLine()
                   .AppendLine($"[{nameof(ExcludedSymbols)}]")
                   .Append($"{string.Join(',', ExcludedSymbols.Select(u => u.ToString()))}");

            return builder.ToString();
        }


        public enum PriceType
        {
            Percents,
            Pips,
        }


        public sealed class PriceSetting
        {
            public const double DefaultTP = 0.03;

            public static PriceSetting Default { get; } = new PriceSetting(DefaultTP);


            public PriceType Type { get; }

            public double Value { get; }


            private PriceSetting(double value)
            {
                Type = PriceType.Percents;
                Value = value;
            }

            internal PriceSetting(string str)
            {
                Type = IsPips(ref str, "p") || IsPips(ref str, "pips") ? PriceType.Pips : PriceType.Percents;

                if (Parser.TryGetDouble(str, out var value))
                    Value = value;
                else
                    throw new ValidationException($"Cannot convert value to percent or pips format: {str}");
            }


            private static bool IsPips(ref string str, string pattern)
            {
                var isPips = str.EndsWith(pattern);

                if (isPips)
                    str = str[..^pattern.Length];

                return isPips;
            }


            public override string ToString() => $"{Value}{(Type == PriceType.Pips ? "pips" : "")}";
        }


        public sealed class SymbolSetting
        {
            public const double DefaultMinVolume = 1.0;

            private readonly static char[] _splitters = new char[] { ';', '=' };

            private readonly string _symbol;


            public PriceSetting TakeProfit { get; private set; } = PriceSetting.Default;

            public double MinVolume { get; private set; } = DefaultMinVolume;


            public SymbolSetting() { }

            public SymbolSetting(KeyValuePair<string, string> pair, double defaultVolume)
            {
                _symbol = pair.Key;

                MinVolume = defaultVolume;

                var parts = pair.Value.ToLower().Split(_splitters, StringSplitOptions.RemoveEmptyEntries).Select(u => u.Trim()).ToList();

                if (parts.Count != 2 && parts.Count != 4)
                    ThrowValidationException(pair.Value);

                for (int i = 0; i < parts.Count; i += 2)
                {
                    var rawValue = parts[i + 1];

                    switch (parts[i])
                    {
                        case "tp":
                            TakeProfit = new PriceSetting(rawValue);
                            break;
                        case "minvolume":
                            if (!Parser.TryGetDouble(rawValue, out double value))
                                throw new ValidationException($"Cannot convert {rawValue} to double. Line: {_symbol} {pair.Value}");
                            MinVolume = value;
                            break;
                        default:
                            ThrowValidationException(pair.Value);
                            break;
                    }
                }
            }

            private void ThrowValidationException(string str) =>
                throw new ValidationException($"Invalid line format:{_symbol} {str}. Correct format: TP=val; MinVolume=val");

            public override string ToString() => $"{_symbol}: TP={TakeProfit} MinVolume={MinVolume}";
        }
    }
}