namespace _100YearPortfolio.Clients
{
    public static class ClientFactory
    {
        internal static BaseSheetClient GetClient(string link, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return new HttpSheetClient(link);
            else
                return new GoogleSheetApiClient(link, apiKey);
        }
    }
}