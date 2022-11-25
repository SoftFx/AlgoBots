using _100YearPortfolio.Clients;
using _100YearPortfolio.Portfolio;
using System.Text;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;
using File = TickTrader.Algo.Api.File;

namespace _100YearPortfolio
{
    [TradeBot(DisplayName = FullBotName, Category = "SoftFX Public", Version = "1.0")]
    public class PortfolioBot : TradeBot
    {
        private const int ErrorTimeout = 30000;

        public const string FullBotName = "100YearPortfolioBot";

        private BaseSheetClient _client;
        private PortfolioConfig _config;
        private MarketState _market;

        private RecalculationEvent _marketState;
        private RecalculationEvent _equityState;

        private double _lastCalculatedEquity;
        private string _balancePrecision;


        [Parameter(IsRequired = true)]
        public string SheetLink { get; set; }

        [Parameter(IsRequired = false)]
        [FileFilter("Creds file (*.json)", "*.json")]
        public File CredsFile { get; set; }


        internal PortfolioConfig Config => _config;

        internal double CalculationBalance => Config.BalanceType.IsBalance() ? Account.Balance : Account.Equity;

        internal double EquityChange => 100.0 * (1.0 - Account.Equity / _lastCalculatedEquity);


        protected override void Init()
        {
            string GetBalanceFormat()
            {
                return $"F{Currencies[Account.BalanceCurrency].Digits}";
            }

            _lastCalculatedEquity = Account.Equity;

            _market = new MarketState(this);
            _client = ClientFactory.GetClient(SheetLink, CredsFile.FullPath);

            if (!TryValidateAccountSettings(out var error) ||
                !_client.TryReadConfig(out _config, out error) ||
                !_client.TryFillMarket(_market, out error))
                StopBotWithError(error);
            else
            {
                _balancePrecision = GetBalanceFormat();

                _marketState = new RecalculationEvent
                {
                    ChangeTimeAction = t => t.AddMinutes(Config.UpdateMinutes),
                    RecalculateAction = _market.Recalculate,
                };

                _equityState = new RecalculationEvent
                {
                    ChangeTimeAction = t => t.AddSeconds(Config.EquityUpdateTime),
                    RecalculateAction = RememberEquity,
                };

                Account.BalanceUpdated += AccountBalanceUpdated;
                Account.Orders.Opened += OrdersOpened;

                _ = UpdateLoop();
            }
        }

        protected override void OnStop() => _client?.Dispose();


        private async Task UpdateLoop()
        {
            if (await FlushSheetStatus())
                while (!IsStopped)
                {
                    try
                    {
                        await _marketState.Recalculate(UtcNow);
                        await _equityState.Recalculate(UtcNow);

                        if (EquityChange.Lt(Config.EquityMinLevel))
                        {
                            await CriticalLossMoney();
                            break;
                        }

                        var currentStatus = BuildCurrentStatus();

                        Status.WriteLine(currentStatus);

                        await _client.SendStatus(currentStatus);
                        await Delay(Config.StatusUpdateTimeoutSec * 1000);

                        Status.Flush();
                    }
                    catch (Exception ex)
                    {
                        PrintError(ex.Message);

                        await Delay(ErrorTimeout);
                    }
                }
        }

        private void AccountBalanceUpdated() => _marketState.Recalculate(UtcNow);

        private void OrdersOpened(OrderOpenedEventArgs obj) => Status.WriteLine(BuildCurrentStatus());

        private string BuildCurrentStatus()
        {
            var sb = new StringBuilder(1 << 10);

            sb.AppendLine($"{UtcNow}").AppendLine()
              .AppendLine($"{Config}").AppendLine()
              .AppendLine($"Account:")
              .AppendLine($"{nameof(Account.Balance)} = {Account.Balance.ToString(_balancePrecision)}")
              .AppendLine($"{nameof(Account.Equity)} = {Account.Equity.ToString(_balancePrecision)}")
              .AppendLine()
              .AppendLine($"{_market}")
              .AppendLine($"Saved equity = {_lastCalculatedEquity.ToString(_balancePrecision)}, equity change: {EquityChange:F2}%")
              .AppendLine($"Resave equity value after {_equityState.GetLeftTime(UtcNow)} sec...")
              .AppendLine()
              .AppendLine($"Recalculation symbols after {_marketState.GetLeftTime(UtcNow)} sec...");

            return sb.ToString();
        }


        private Task RememberEquity()
        {
            _lastCalculatedEquity = Account.Equity;

            return Task.CompletedTask;
        }

        private bool TryValidateAccountSettings(out string error)
        {
            error = null;

            if (Account.Type != AccountTypes.Net)
                error = $"Only Net account is available";

            return string.IsNullOrEmpty(error);
        }

        private async Task CriticalLossMoney()
        {
            Status.Flush();

            await FlushSheetStatus();

            var sb = new StringBuilder(1 << 10);

            sb.AppendLine($"{UtcNow}").AppendLine()
              .AppendLine($"Current Equity change = {EquityChange:F2}%")
              .AppendLine($"{PortfolioConfig.EquityMinLevelSettingName} = {Config.EquityMinLevel}%")
              .AppendLine("Bot has been stopped!");

            var str = sb.ToString();

            await _client.SendStatus(str);

            Alert.Print(str);

            StopBotWithError(str);
        }

        private async Task<bool> FlushSheetStatus()
        {
            try
            {
                await _client.FlushStatus();

                return true;
            }
            catch (Exception ex)
            {
                StopBotWithError(ex.Message);

                return false;
            }
        }

        private void StopBotWithError(string error)
        {
            Account.BalanceUpdated -= AccountBalanceUpdated;

            PrintError(error);
            Status.WriteLine(error);
            Exit();
        }
    }
}