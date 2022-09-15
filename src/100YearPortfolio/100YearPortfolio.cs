using _100YearPortfolio.Clients;
using _100YearPortfolio.Portfolio;
using SoftFx;
using System.Text;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace _100YearPortfolio
{
    [TradeBot(DisplayName = FullBotName, Category = CommonConstants.Category, Version = "1.0")]
    public class PortfolioBot : TradeBot
    {
        private const int StatusUpdateTimeout = 1000;

        public const string FullBotName = "100YearPortfolioBot";


        [Parameter]
        public bool UseDebug { get; set; }

        //[Parameter(DefaultValue = "AIzaSyDFPzNAaQgYaWUHuKUSLAHnlumB_rLT2CA")]
        [Parameter]
        public string ApiKey { get; set; }


        //[Parameter(DefaultValue = "https://docs.google.com/spreadsheets/d/1VstuuH9WYRDlUpNiagz026C6gfYwUFoOVuR8I5XJCgw/edit#gid=0")]
        [Parameter]
        public string SheetLink { get; set; }


        internal PortfolioConfig Config => _config;

        public double CalculationBalance => Config.BalanceType.IsBalance() ? Account.Balance : Account.Equity;

        public double EquityChange => MarketSymbol.PercentCoef * (1.0 - Account.Equity / _lastCalculatedEquity);


        private BaseSheetClient _client;
        private PortfolioConfig _config;
        private MarketState _market;

        private RecalculationEvent _marketState;
        private RecalculationEvent _equityState;

        private double _lastCalculatedEquity;
        private string _balancePrecision;


        protected override void Init()
        {
            _lastCalculatedEquity = Account.Equity;

            _client = ClientFactory.GetClient(SheetLink, ApiKey);

            if (TryValidateGlobalState(out var error) || !_client.TryReadConfig(out _config, out error) ||
                !_client.TryReadPortfolio(this, out _market, out error))
            {
                PrintError(error);
                Status.WriteLine(error);
                Exit();
            }
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

                ThreadPool.QueueUserWorkItem(UpdateLoop);
            }
        }

        protected override void OnStop() => _client?.Dispose();


        private async void UpdateLoop(object _)
        {
            while (!IsStopped)
            {
                var currentStatus = BuildCurrentStatus();

                Status.WriteLine(currentStatus);

                await _marketState.Recalculate(UtcNow);
                await _equityState.Recalculate(UtcNow);

                if (EquityChange.Lt(Config.EquityMinLevel))
                {
                    CriticalLossMoney();
                    break;
                }

                Status.WriteLine(BuildDeltaInfo());

                await Delay(StatusUpdateTimeout);

                Status.Flush();
            }
        }

        private string BuildCurrentStatus()
        {
            var sb = new StringBuilder(1 << 10);

            sb.AppendLine($"{UtcNow}").AppendLine()
              .AppendLine($"{Config}").AppendLine()
              .AppendLine($"Account:")
              .AppendLine($"{nameof(Account.Balance)} = {Account.Balance.ToString(_balancePrecision)}")
              .AppendLine($"{nameof(Account.Equity)} = {Account.Equity.ToString(_balancePrecision)}")
              .AppendLine()
              .AppendLine($"{_market}");

            return sb.ToString();
        }

        private string BuildDeltaInfo()
        {
            var sb = new StringBuilder(1 << 5);

            sb.AppendLine($"Saved equity = {_lastCalculatedEquity:F4}, equity change: {EquityChange:F4}%")
              .AppendLine($"Resave equity value after {_equityState.GetLeftTime(UtcNow)} sec...")
              .AppendLine()
              .AppendLine($"Recalculation symbols after {_marketState.GetLeftTime(UtcNow)} sec...");

            return sb.ToString();
        }

        internal void PrintDebug(string msg)
        {
            if (UseDebug)
                Print(msg);
        }

        private Task RememberEquity()
        {
            _lastCalculatedEquity = Account.Equity;

            return Task.CompletedTask;
        }

        private bool TryValidateGlobalState(out string error)
        {
            error = null;

            if (Account.Type != AccountTypes.Net)
                error = $"Only Net account is available";

            return !string.IsNullOrEmpty(error);
        }

        private string GetBalanceFormat()
        {
            var smb = Currencies[Account.BalanceCurrency];

            return $"F{smb.Digits}";
        }

        private void CriticalLossMoney()
        {
            Status.Flush();

            Status.WriteLine($"Current Equity change = {EquityChange:F4}%");
            Status.WriteLine($"{PortfolioConfig.EquityMinLevelSettingName} = {Config.EquityMinLevel}%");
            Status.WriteLine("Bot has been stopped!");

            Exit();
        }
    }
}