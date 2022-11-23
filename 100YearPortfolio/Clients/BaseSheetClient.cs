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

        protected const string NoteRange = $"{PortfolioPage}!A1:A";

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
            else if (!_reader.TryReadConfig(configStr, out config, out error))
                error = $"Cannot read bot settings. Sheet {ConfigPage}. {error}";

            return string.IsNullOrEmpty(error);
        }

        internal bool TryFillMarket(MarketState market, out string error)
        {
            if (!TryReadPage(PortfolioPage, out var portfolioStr))
                error = EmptySheetError(PortfolioPage);
            else if (!TryReadNotes(out var notes, out error) || !PortfolioReader.TryFillMarket(portfolioStr, notes, market, out error))
                error = $"Cannot read bot portfolio. Sheet {PortfolioPage}. {error}";

            return string.IsNullOrEmpty(error);
        }

        internal virtual Task SendStatus(string status) => Task.CompletedTask;

        internal virtual Task FlushStatus() => Task.CompletedTask;


        protected abstract bool TryReadPage(string pageName, out List<List<string>> configStr);

        protected abstract bool TryReadNotes(out List<string> settingsStr, out string error);

        protected abstract bool IsValidLink();

        public abstract void Dispose();


        private static string EmptySheetError(string sheet) => $"{sheet} sheet is empty!";
    }
}