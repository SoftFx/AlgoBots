using _100YearPortfolio.Portfolio;
using System.Text.RegularExpressions;

namespace _100YearPortfolio.Clients
{
    internal abstract class BaseSheetClient : IDisposable
    {
        private const string SpreadSheetIdRegPattern = @"/d/(.+)/";

        protected const string ConfigPage = "Settings";
        protected const string PortfolioPage = "Portfolio";
        protected const string StatusPage = "Status";

        private readonly PortfolioReader _reader = new();
        private readonly string _sheetLink;

        protected readonly string _spreadSheetId;


        protected BaseSheetClient(string link)
        {
            _sheetLink = link;

            _reader = new PortfolioReader();

            var match = new Regex(SpreadSheetIdRegPattern).Match(link);

            if (match.Success && match.Groups.Count > 1)
                _spreadSheetId = match.Groups[1].Value;
        }


        internal bool TryReadConfig(out PortfolioConfig config, out string error)
        {
            config = null;

            if (!IsValidLink())
                error = $"Cannot get spread sheet id or read data from {_sheetLink}!";
            else if (!TryReadPage(ConfigPage, out var configStr))
                error = EmptySheetError(ConfigPage);
            else if (!_reader.TryReadSettings(configStr, out config, out error))
                error = $"Cannot read bot settings. Sheet {ConfigPage}. {error}";

            return string.IsNullOrEmpty(error);
        }

        internal bool TryReadPortfolio(PortfolioBot bot, out MarketState state, out string error)
        {
            state = null;

            if (!TryReadPage(PortfolioPage, out var portfolioStr))
                error = EmptySheetError(PortfolioPage);
            else if (!PortfolioReader.TryReadPortfolio(bot, portfolioStr, out state, out error))
                error = $"Cannot read bot portfolio. Sheet {PortfolioPage}. {error}";

            return string.IsNullOrEmpty(error);
        }

        internal virtual Task SendStatus(string status)
        {
            return Task.CompletedTask;
        }


        protected abstract bool TryReadPage(string pageName, out List<List<string>> configStr);

        protected abstract bool IsValidLink();

        public abstract void Dispose();


        private static string EmptySheetError(string sheet) => $"{sheet} sheet is empty!";
    }
}