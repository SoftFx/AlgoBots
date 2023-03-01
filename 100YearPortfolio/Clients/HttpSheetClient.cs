using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace _100YearPortfolio.Clients
{
    internal sealed class HttpSheetClient : BaseSheetClient
    {
        private const string ExportExcelUrl = @"https://docs.google.com/spreadsheets/export?id=";

        private readonly HttpClient _httpClient = new();
        private readonly XSSFWorkbook _book;


        internal HttpSheetClient(string link) : base(link)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ExportExcelUrl}{_spreadSheetId}");
            var response = _httpClient.Send(request);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    _book = new XSSFWorkbook(response.Content.ReadAsStream());
                }
                catch
                {
                    throw new Exception("Can't parse file data. Please check the link permissions.");
                }
            }
        }


        protected override bool TryReadPage(string pageName, out List<List<string>> configStr)
        {
            configStr = new List<List<string>>();

            var sheet = _book.GetSheet(pageName);

            if (sheet == null)
                return false;

            for (int i = sheet.FirstRowNum; i <= sheet.LastRowNum; i++)
            {
                var usedCells = GetUsedCells(sheet.GetRow(i));

                if (usedCells.Count > 0)
                    configStr.Add(usedCells.Select(c => $"{c}").ToList());
            }

            return configStr.Count > 0;
        }

        protected override bool TryReadNotes(out List<string> settingsStr, out string error)
        {
            error = null;
            settingsStr = new List<string>();

            var sheet = _book.GetSheet(PortfolioPage);

            if (sheet == null)
                return PortfolioPageNotFound(out error);

            for (int i = sheet.FirstRowNum; i <= sheet.LastRowNum; i++)
            {
                var usedCells = GetUsedCells(sheet.GetRow(i));

                if (usedCells.Count > 0)
                    settingsStr.Add(usedCells[0].CellComment?.String.String);
            }

            return true;
        }


        protected override bool IsValidLink() => _book != null;

        public override void Dispose()
        {
            _book?.Dispose();
            _httpClient?.Dispose();
        }


        private static List<ICell> GetUsedCells(IRow row)
        {
            return (row?.Cells ?? Enumerable.Empty<ICell>()).ToList();
        }
    }
}