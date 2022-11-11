using static _100YearPortfolio.Portfolio.PortfolioConfig;

namespace _100YearPortfolio
{
    internal static class Extensions
    {
        public static bool IsBalance(this BalanceTypeEnum type) => type == BalanceTypeEnum.Balance;
    }
}