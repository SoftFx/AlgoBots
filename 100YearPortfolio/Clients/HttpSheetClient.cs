﻿using ClosedXML.Excel;

namespace _100YearPortfolio.Clients
{
    internal sealed class HttpSheetClient : BaseSheetClient
    {
        private const string ExportExcelUrl = @"https://docs.google.com/spreadsheets/export?id=";

        private readonly HttpClient _httpClient = new();
        private readonly XLWorkbook _book;


        internal HttpSheetClient(string link) : base(link)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ExportExcelUrl}{_spreadSheetId}");
            var response = _httpClient.Send(request);

            if (response.IsSuccessStatusCode)
            {
                using var reader = response.Content.ReadAsStream();

                _book = new XLWorkbook(reader);
            }
        }


        protected override bool TryReadPage(string pageName, out List<List<string>> configStr)
        {
            configStr = new List<List<string>>();

            if (!_book.TryGetWorksheet(pageName, out var sheet))
                return false;

            foreach (var row in sheet.RowsUsed())
            {
                var cells = row.Cells();

                cells.DataType = XLDataType.Text;

                configStr.Add(cells.Select(c => c.GetString()).ToList());
            }

            return configStr.Count > 0;
        }

        protected override bool IsValidLink() => _book != null;

        public override void Dispose()
        {
            _book?.Dispose();
            _httpClient?.Dispose();
        }
    }
}