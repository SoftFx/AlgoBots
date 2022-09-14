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

        internal const string UpdateHoursSettingName = "Once Per N hour";
        internal const string BalanceTypeSettingName = "Balance Type";
        internal const string EquityMinLevelSettingName = "Equity Min Lvl";
        internal const string EquityUpdateTimeName = "Equity Update Time (sec)";
        internal const string DefaultMaxLotSizeSettingName = "Default Max Lots Sum";

        private string _toString;


        public int UpdateHours { get; init; }

        public BalanceTypeEnum BalanceType { get; init; }

        public double EquityMinLevel { get; init; }

        public int EquityUpdateTime { get; init; }

        public double DefaultMaxLotSize { get; init; }


        public override string ToString()
        {
            string BuildString()
            {
                var sb = new StringBuilder(1 << 8);

                sb.AppendLine($"{nameof(PortfolioConfig)}:")
                  .AppendLine($"{UpdateHoursSettingName} = {UpdateHours}")
                  .AppendLine($"{BalanceTypeSettingName} = {BalanceType}")
                  .AppendLine($"{EquityMinLevelSettingName} = {EquityMinLevel}%")
                  .AppendLine($"{EquityUpdateTimeName} = {EquityUpdateTime}")
                  .Append($"{DefaultMaxLotSizeSettingName} = {DefaultMaxLotSize}");

                return sb.ToString();
            }

            _toString ??= BuildString();

            return _toString;
        }
    }
}