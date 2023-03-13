using SoftFx.Routines;
using System;
using System.Text;
using System.Threading.Tasks;
using TickTrader.Algo.Api;

namespace TPtoAllNewPositionsInPercents
{
    [TradeBot(Category = "SoftFX Public", DisplayName = "TPtoAllNewPositionsInPercents", Version = "1.3",
              Description = "The bot emulates the TakeProfit for positions on Net account. Every time interval it checks the amount of positions and" +
                            " updates the limit orders set so that the Amount of position opened by a symbol is equivalent the amount of limit orders.")]
    public class TPtoAllNewPositionsInPercents : SingleLoopBot<TPtoAllNewPositionsPercentsConfiguration>
    {
        private const string ConfigDefaultFileName = "TPtoAllNewPositionsInPercents.tml";

        private LimitWatcher _limitWatcher;


        internal string CommentPrefix => $"{Id}-";


        [Parameter(DisplayName = "Config File", DefaultValue = ConfigDefaultFileName)]
        [FileFilter("Toml Config (*.tml)", "*.tml")]
        public File ConfigFile { get; set; }


        protected override Task InitInternal()
        {
            LoopTimeout = Config.RunIntervalInSeconds * 1000;

            if (Account.Type != AccountTypes.Net)
            {
                PrintError("Bot works only on Net account");
                Abort();
            }

            _limitWatcher = new LimitWatcher(this);
            Account.NetPositions.Modified += _limitWatcher.UploadPosition;

            return base.InitInternal();
        }

        protected override void OnStop()
        {
            Account.NetPositions.Modified -= _limitWatcher.UploadPosition;
        }

        protected override Task Iteration()
        {
            CheckConfigSymbols(); //Display error and warning messages in the status window
            _limitWatcher.UploadPosition();

            return Task.CompletedTask;
        }

        protected override string GetConfigFullPath() => ConfigFile.FullPath;

        protected override string GetDefaultConfigFileName() => ConfigDefaultFileName;


        private void CheckConfigSymbols()
        {
            var str = new StringBuilder(1 << 10);

            foreach (var key in Config.SymbolsSettings.Keys)
                if (Symbols[key].IsNull)
                    str.AppendLine($"Symbol {key} not found on server.");

            if (Config.HasErrors)
                Status.WriteLine($"Config error:{Environment.NewLine}{Config.GetAllErrors}");

            if (str.Length > 0)
                Status.WriteLine($"Config warning:{Environment.NewLine}{str}");
        }
    }
}
