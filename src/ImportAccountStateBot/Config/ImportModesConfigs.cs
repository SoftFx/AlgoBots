using System.Text;

namespace ImportAccountStateBot
{
    public sealed class MarketModeConfig
    {

    }

    public sealed class TrailingLimitPercentModeConfig
    {
        public double Percent { get; set; }


        public override string ToString()
        {
            var sb = new StringBuilder(1 << 6);

            sb.AppendLine($"{nameof(Percent)} = {Percent}");

            return sb.ToString();
        }
    }
}
