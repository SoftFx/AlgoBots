using SoftFx.Routines;
using SoftFx;
using System;
using System.Threading.Tasks;
using TickTrader.Algo.Api;

namespace ImportAccountStateBot
{
    public enum ImportMode
    {
        Market,
        TrailingLimit,
        TrailingLimitPercent,
    }

    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }


    [TradeBot(Category = CommonConstants.Category, SetupMainSymbol = false, DisplayName = "ImportAccountStateBot", Version = "1.0")]
    public class ImportAccountStateBot : SingleLoopBot<ImportAccountStateBotConfig>, ITimeProvider
    {
        private const string ConfigFileName = "ImportAccountStateBot.tml";
        private const string DefaultAccountStateFileName = "AccountState.csv";

        private AccountStateFileParser _csvParser;
        private OrderWatchersManager _orderManager;
        private AccountStateMachine _accStateMachine;


        [Parameter(IsRequired = true, DefaultValue = DefaultAccountStateFileName)]
        [FileFilter("CSV file (*.csv)", "*.csv")]
        public File StateFile { get; set; }

        [Parameter(DisplayName = "Config File", DefaultValue = ConfigFileName)]
        [FileFilter("Toml Config (*.tml)", "*.tml")]
        public File ConfigFile { get; set; }


        protected override string GetConfigFullPath() => ConfigFile.FullPath;

        protected override string GetDefaultConfigFileName() => ConfigFileName;


        protected async override Task InitInternal()
        {
            ValidateAccount();

            IsDebug = Config.IsDebug;
            LoopTimeout = Config.RefreshTimeout;

            _csvParser = new AccountStateFileParser(this);
            _orderManager = new OrderWatchersManager(this);

            _accStateMachine = new AccountStateMachine(this);
            _accStateMachine.PushToken += _orderManager.GetTokenHandler; //subscription shoul be before Init method

            await InitCurrentAccountState();

            Connected += ConnectEventHandler;
        }

        protected override Task Iteration()
        {
            PrintCurrentStatus();
            IsTimeToExitBot();

            if (_csvParser.HasNewData)
                _accStateMachine?.AddAccountStates(_csvParser.ReadAccountStates());

            _accStateMachine?.ToNextAccountState();

            _orderManager?.ApplyAllTokens();
            _orderManager?.CorrectAllOrders();

            return Task.CompletedTask;
        }

        private void ValidateAccount()
        {
            if (Account.Type != AccountTypes.Net)
            {
                PrintError($"Bot supports only Net account");
                Exit();
            }
        }

        private async Task InitCurrentAccountState()
        {
            Status.WriteLine("Canceling old pendings...");
            await _orderManager.ClearAllWatchers();

            Status.WriteLine("Initialization...");
            _accStateMachine?.AddAccountStates(_csvParser.ReadAccountStates());
            _accStateMachine?.InitCurrentState(Account.NetPositions);
        }

        private void PrintCurrentStatus()
        {
            Status.WriteLine($"{_accStateMachine?.CurrentMachineStateString() ?? "Loading..."}");
            Status.WriteLine($"{_orderManager}");
        }

        private void IsTimeToExitBot()
        {
            if (_accStateMachine.ExpectedState == null && _orderManager.AllWatchersIsEmpty())
            {
                Print($"All account states have been processed");
                Exit();
            }
        }

        private async void ConnectEventHandler(object sender, ConnectedEventArgs e) =>
            await InitCurrentAccountState();
    }
}
