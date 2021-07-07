using System.Text;

namespace ImportAccountStateBot
{
    public sealed class CSVFileConfig
    {
        public double DefaultVolume { get; set; }

        public string TimeFormat { get; set; }

        public string Separator { get; set; }

        public bool SkipFirstLine { get; set; }


        public override string ToString()
        {
            var sb = new StringBuilder(1 << 6);

            sb.AppendLine($"{nameof(DefaultVolume)} = {DefaultVolume}");
            sb.AppendLine($"{nameof(TimeFormat)} = {TimeFormat}");
            sb.AppendLine($"{nameof(Separator)} = {Separator}");
            sb.AppendLine($"{nameof(SkipFirstLine)} = {SkipFirstLine}");

            return sb.ToString();
        }
    }
}
