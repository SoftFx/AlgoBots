using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace _100YearPortfolio.Clients
{
    internal sealed class GoogleSheetApiClient : BaseSheetClient
    {
        private readonly SheetsService _service;


        internal GoogleSheetApiClient(string link, string apiKey) : base(link)
        {
            _service = new SheetsService(new BaseClientService.Initializer
            {
                ApiKey = apiKey,
                ApplicationName = PortfolioBot.FullBotName,
            });

        }


        protected override bool TryReadPage(string pageName, out List<List<string>> configStr)
        {
            var request = _service.Spreadsheets.Values.Get(_spreadSheetId, pageName);
            var values = request.Execute().Values;

            var result = values != null && values.Count > 0;

            configStr = result ? new List<List<string>>(values.Count) : null;

            if (result)
            {
                foreach (var value in values)
                    if (value.Count > 0)
                        configStr.Add(new List<string>(value.OfType<string>()));
            }

            return result && configStr.Count > 0;
        }


        protected override bool IsValidLink() => !string.IsNullOrEmpty(_spreadSheetId);

        public override void Dispose() => _service.Dispose();
    }
}
