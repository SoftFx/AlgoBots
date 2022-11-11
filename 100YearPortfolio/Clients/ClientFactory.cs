namespace _100YearPortfolio.Clients
{
    public static class ClientFactory
    {
        internal static BaseSheetClient GetClient(string link, string credsPath)
        {
            if (string.IsNullOrEmpty(credsPath))
                return new HttpSheetClient(link);
            else
                return new GoogleSheetApiClient(link, credsPath);
        }
    }
}