using SoftFx.Routines;
using System.Text;
using System;
using System.Threading.Tasks;
using TickTrader.Algo.Api;

namespace TPtoAllNewPositions
{
    [TradeBot(Category = "SoftFX Public", DisplayName = "TPtoAllNewPositions", Version = "1.0",
              Description = "The bot emulates the TakeProfit for positions on Net account. It sets the specified TP for all new Net positions")]
    public class TPtoAllNewPositions : SingleLoopBot<TPtoAllNewPositionsConfiguration>
    {
        private const string ConfigDefaultFileName = $"{nameof(TPtoAllNewPositions)}.tml";


        [Parameter(DisplayName = "Config File", DefaultValue = $"{nameof(TPtoAllNewPositions)}.tml")]
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

            return base.InitInternal();
        }

        protected override Task Iteration()
        {
            CheckConfigSymbols();
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