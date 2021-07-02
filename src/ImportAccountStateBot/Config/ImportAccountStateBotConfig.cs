using SoftFX;
using System.Text;

namespace ImportAccountStateBot
{
    public sealed class ImportAccountStateBotConfig : BotConfig
    {
        private string _configToString;


        public bool IsDebug { get; set; }

        public int RefreshTimeout { get; set; }

        public ImportMode Mode { get; set; }

        public bool SetEmptyStateAtTheEnd { get; set; }


        public CSVFileConfig CSVConfig { get; set; }

        public TrailingLimitPercentModeConfig TrailingLimitPercentMode { get; set; }


        public ImportAccountStateBotConfig()
        {
            IsDebug = false;
            RefreshTimeout = 1000;
            Mode = ImportMode.Market;
            SetEmptyStateAtTheEnd = false;

            CSVConfig = new CSVFileConfig
            {
                DefaultVolume = 1.0,
                Separator = ",",
                TimeFormat = "yyyy-MM-dd'T'H:mm:ss'Z'",
                SkipFirstLine = true,
            };

            TrailingLimitPercentMode = new TrailingLimitPercentModeConfig
            {
                Percent = 0.1,
            };
        }

        public override void Init()
        {
            _configToString = ConfigToString();

            ValidationConfig();
        }

        private void ValidationConfig()
        {
            Rule.CheckNumberGt(nameof(RefreshTimeout), RefreshTimeout, 0);
            Rule.CheckNumberGt(nameof(CSVConfig.DefaultVolume), CSVConfig.DefaultVolume, 0.0);

            Rule.CheckNumberGte(nameof(TrailingLimitPercentMode.Percent), TrailingLimitPercentMode.Percent, 0);
            //Rule.CheckNumberLte(nameof(TrailingLimitsPercentMode.Percent), TrailingLimitsPercentMode.Percent, 100.0);
        }

        private string ConfigToString()
        {
            var sb = new StringBuilder(1 << 10);

            sb.AppendLine($"{nameof(IsDebug)} = {IsDebug}");
            sb.AppendLine($"{nameof(RefreshTimeout)} = {RefreshTimeout}");
            sb.AppendLine($"{nameof(Mode)} = {Mode}");
            sb.AppendLine($"{nameof(SetEmptyStateAtTheEnd)} = {SetEmptyStateAtTheEnd}");
            sb.AppendLine();

            sb.AppendLine($"[[{nameof(CSVConfig)}]]");
            sb.AppendLine($"{CSVConfig}");

            sb.AppendLine($"[[{nameof(TrailingLimitPercentMode)}]]");
            sb.Append($"{TrailingLimitPercentMode}");

            return sb.ToString();
        }

        public override string ToString() => _configToString;
    }
}
