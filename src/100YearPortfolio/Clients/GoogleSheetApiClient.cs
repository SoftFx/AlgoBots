using Google.Apis.Auth.OAuth2;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;

namespace _100YearPortfolio.Clients
{
    internal sealed class GoogleSheetApiClient : BaseSheetClient
    {
        private static readonly string[] _scopes = { SheetsService.Scope.Spreadsheets };

        private readonly ClearRequest _clearRequest;
        private readonly SheetsService _service;


        internal GoogleSheetApiClient(string link, string credPath) : base(link)
        {
            _service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleCredential.FromFile(credPath).CreateScoped(_scopes),
                ApplicationName = PortfolioBot.FullBotName,
            });

            _clearRequest = _service.Spreadsheets.Values.Clear(new ClearValuesRequest(), _spreadSheetId, StatusPage);
        }


        internal override Task SendStatus(string status)
        {
            var valueRange = new ValueRange
            {
                Values = status.Split(Environment.NewLine).Select(u => (IList<object>)new List<object>(1) { u }).ToList(),
            };

            var updateRequest = _service.Spreadsheets.Values.Update(valueRange, _spreadSheetId, StatusPage);

            updateRequest.ValueInputOption = UpdateRequest.ValueInputOptionEnum.RAW;

            return updateRequest.ExecuteAsync();
        }

        internal override Task FlushStatus() => _clearRequest.ExecuteAsync();


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
