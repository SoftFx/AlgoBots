using SoftFx;
using TickTrader.Algo.Api;
using TradeFile = TickTrader.Algo.Api.File;

namespace _100YearPortfolio
{
    [TradeBot(DisplayName = "100YearPortfolioBot", Category = CommonConstants.Category, Version = "1.0")]
    public class PortfolioBot : TradeBot
    {
        private const int StatusUpdateTimeout = 1000;

        public enum BalanceTypeEnum
        {
            Balance,
            Equity
        }

        [Parameter(DisplayName = "Portfolio", DefaultValue = "DesiredStockDistribution.csv", IsRequired = true)]
        [FileFilter("CSV Config (*.csv)", "*csv")]
        public TradeFile PortfolioFile { get; set; }

        [Parameter(DisplayName = "CSV separator", DefaultValue = ";")]
        public string Separator { get; set; }

        [Parameter(DisplayName = "Once per N hour", DefaultValue = 24)]
        public int UpdateHours { get; set; }

        [Parameter]
        public BalanceTypeEnum BalanceType { get; set; }

        [Parameter]
        public bool UseDebug { get; set; }


        public double CalculationBalance => BalanceType == BalanceTypeEnum.Balance ? Account.Balance : Account.Equity;


        private MarketState _marketState;


        protected override void Init()
        {
            var reader = new PortfolioReader(this);

            if (!reader.TryRead(PortfolioFile, out _marketState))
                Exit();

            ThreadPool.QueueUserWorkItem(UpdateLoop);
        }

        private async void UpdateLoop(object _)
        {
            while (!IsStopped)
            {
                Status.WriteLine($"{UtcNow}");
                Status.WriteLine();
                Status.WriteLine($"{nameof(BalanceType)}={BalanceType}");

                Status.WriteLine();
                Status.WriteLine("Symbols:");
                Status.WriteLine(_marketState.BuildCurrentState());

                await _marketState.Recalculate();
                await Delay(StatusUpdateTimeout);

                Status.Flush();
            }
        }

        internal void PrintDebug(string msg)
        {
            if (UseDebug)
                Print(msg);
        }
    }
}