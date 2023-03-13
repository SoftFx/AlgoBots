using SoftFx;
using SoftFx.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TPtoAllNewPositionsInPercents
{
    public class TPtoAllNewPositionsPercentsConfiguration : BotConfig
    {
        [Nett.TomlIgnore]
        public Dictionary<string, SymbolSetting> SymbolsSettingsDict { get; } = new Dictionary<string, SymbolSetting>();

        public Dictionary<string, string> SymbolsSettings { get; set; }


        public int RunIntervalInSeconds { get; set; }

        public double DefaultMinVolume { get; set; }

        public double DefaultTP { get; set; }


        public TPtoAllNewPositionsPercentsConfiguration()
        {
            RunIntervalInSeconds = 3;
            DefaultTP = SymbolSetting.DefaultTP;
            DefaultMinVolume = SymbolSetting.DefaultMinVolume;

            SymbolsSettings = new Dictionary<string, string>
            {
                {"USDRUB", "TP=0.10; MinVolume=0.1"},
                {"GBPJPY", "MinVolume=0.05; TP=0.10" },
                {"AUDUSD", "TP=0.20; MinVolume=0.1;" },
                {"EURJPY", "TP=0.10;" },
                {"AUDNZD", "TP=0.05" },
                {"GBPAUD", "MinVolume=0.1; TP=0.15;" },
                {"GBPUSD", "TP=0.10; MinVolume=0.3" },
                {"NZDUSD", "TP= 0.10; MinVolume = 0.22" },
                {"USDCNH", "TP=0.10; MINVOLUME=0.1" },
                {"EURCHF", "tp=0.03; minvolume=0.03" },
                {"EURUSD", "MinVolume=0.1;" },
                {"HKDJPY", "MinVolume=0.2" },
            };
        }

        public override void Init()
        {
            if (DefaultTP < 0.0)
                throw new ValidationException($"{nameof(DefaultTP)} must be greater or equal than 0");

            if (DefaultMinVolume <= 0.0)
                throw new ValidationException($"{nameof(DefaultMinVolume)} must be greater than 0");

            if (RunIntervalInSeconds <= 0)
                throw new ValidationException($"{nameof(RunIntervalInSeconds)} must be greater than 0");

            foreach (var pair in SymbolsSettings)
                SymbolsSettingsDict.Add(pair.Key, new SymbolSetting(pair));

            ValidateSymbolsSettings();
        }


        public double TryGetTP(string symbol) => SymbolsSettingsDict.TryGetValue(symbol, out var settings) ? settings.TakeProfit : DefaultTP;

        public double TryGetMinVolume(string symbol) => SymbolsSettingsDict.TryGetValue(symbol, out var settings) ? settings.MinVolume : DefaultMinVolume;


        private void ValidateSymbolsSettings()
        {
            var badPairs = new List<string>(1 << 4);

            foreach (var pair in SymbolsSettingsDict)
            {
                var error = new StringBuilder(1 << 5);

                var tp = pair.Value.TakeProfit;
                var volume = pair.Value.MinVolume;

                if (tp < 0.0)
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
                   .AppendLine()
                   .AppendLine($"[{nameof(SymbolsSettings)}]")
                   .Append($"{string.Join($"{Environment.NewLine}", SymbolsSettingsDict.Values.Select(u => u.ToString()))}");

            return builder.ToString();
        }


        public class SymbolSetting
        {
            public const double DefaultTP = 0.03;
            public const double DefaultMinVolume = 1.0;

            private readonly static char[] _splitters = new char[] { ';', '=' };

            private readonly string _symbol;


            public double TakeProfit { get; set; } = DefaultTP;

            public double MinVolume { get; set; } = DefaultMinVolume;


            public SymbolSetting() { }

            public SymbolSetting(KeyValuePair<string, string> pair)
            {
                _symbol = pair.Key;

                var parts = pair.Value.ToLower().Split(_splitters, StringSplitOptions.RemoveEmptyEntries).Select(u => u.Trim()).ToList();

                if (parts.Count != 2 && parts.Count != 4)
                    ThrowValidationException(pair.Value);

                for (int i = 0; i < parts.Count; i += 2)
                {
                    if (!Parser.TryGetDouble(parts[i + 1], out double value))
                        throw new ValidationException($"Cannot convert {parts[i + 1]} to double. Line: {_symbol} {pair.Value}");

                    switch (parts[i])
                    {
                        case "tp":
                            TakeProfit = value;
                            break;
                        case "minvolume":
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
