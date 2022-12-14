using System.Text;

namespace _100YearPortfolio.Portfolio
{
    internal sealed record PortfolioConfig
    {
        public enum BalanceTypeEnum
        {
            Balance,
            Equity
        }

        internal const string UpdateMinSettingName = "Once Per N min";
        internal const string BalanceTypeSettingName = "Balance Type";
        internal const string EquityMinLevelSettingName = "Equity Min Lvl";
        internal const string EquityUpdateTimeName = "Equity Update Time (sec)";
        internal const string StatusUpdateTimeoutName = "Status Update Timeout (sec)";

        internal const int DefaultStatusUpdateTimeout = 60;

        private string _toString;


        public int UpdateMinutes { get; init; }

        public int StatusUpdateTimeoutSec { get; init; } = DefaultStatusUpdateTimeout;

        public BalanceTypeEnum BalanceType { get; init; }

        public double EquityMinLevel { get; init; }

        public int EquityUpdateTime { get; init; }


        public override string ToString()
        {
            string BuildString()
            {
                var sb = new StringBuilder(1 << 8);

                sb.AppendLine($"{nameof(PortfolioConfig)}:")
                  .AppendLine($"{UpdateMinSettingName} = {UpdateMinutes}")
                  .AppendLine($"{BalanceTypeSettingName} = {BalanceType}")
                  .AppendLine($"{EquityMinLevelSettingName} = {EquityMinLevel * 100.0:F4}%")
                  .AppendLine($"{EquityUpdateTimeName} = {EquityUpdateTime}")
                  .AppendLine()
                  .AppendLine("Optional settings:")
                  .AppendLine($"{StatusUpdateTimeoutName} = {StatusUpdateTimeoutSec}");

                return sb.ToString();
            }

            _toString ??= BuildString();

            return _toString;
        }
    }
}