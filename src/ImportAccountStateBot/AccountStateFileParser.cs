using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TickTrader.Algo.Api;

namespace ImportAccountStateBot
{
    public sealed class AccountStateFileParser
    {
        private readonly CSVFileConfig _config;
        private readonly ImportAccountStateBot _bot;
        private readonly string[] _separator;

        private readonly string _stateFilePath;


        public bool HasNewData => System.IO.File.GetLastWriteTimeUtc(_stateFilePath) != LastReadTime;

        public int FileLinesRead { get; private set; }

        public DateTime LastReadTime { get; private set; }


        public AccountStateFileParser(ImportAccountStateBot bot)
        {
            _config = bot.Config.CSVConfig;
            _stateFilePath = bot.StateFile.FullPath;
            _bot = bot;

            if (!System.IO.File.Exists(_stateFilePath))
                throw new Exception($"State file: {_stateFilePath} not found!");

            _separator = new string[] { _config.Separator };

            FileLinesRead = _config.SkipFirstLine ? 1 : 0;  //skip .csv file headers
        }


        public List<AccountState> ReadAccountStates()
        {
            var states = ReadPositionsStates(_stateFilePath);

            return states.OrderBy(u => u.Time).ThenBy(u => u.Symbol)
                         .GroupBy(u => u.Time).Select(u => new AccountState(u.Key, u)).ToList();
        }

        private List<PositionState> ReadPositionsStates(string file)
        {
            var states = new List<PositionState>(1 << 5);

            try
            {
                using (var fs = new FileStream(file, FileMode.Open))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        for (int i = 0; i < FileLinesRead; ++i) //skip read lines
                            sr.ReadLine();

                        while (!sr.EndOfStream)
                            states.Add(ParseStringToPosition(ReadLineAndUpdateCounter(sr)));
                    }
                }

                LastReadTime = System.IO.File.GetLastWriteTimeUtc(_stateFilePath);
            }
            catch (Exception ex)
            {
                _bot.PrintError(ex.ToString());
            }

            return states;
        }

        private string ReadLineAndUpdateCounter(StreamReader sr)
        {
            FileLinesRead++;

            return sr.ReadLine();
        }

        private PositionState ParseStringToPosition(string str)
        {
            var array = str.Split(_separator, StringSplitOptions.RemoveEmptyEntries);

            var time = DateTime.ParseExact(array[0], _config.TimeFormat, CultureInfo.InvariantCulture);
            var symbol = array[1];
            var side = string.Equals(array[2], "true", StringComparison.InvariantCultureIgnoreCase) ? OrderSide.Buy : OrderSide.Sell;
            var volume = array.Length > 3 ? double.Parse(array[3]) : _config.DefaultVolume;

            return new PositionState(time, symbol, side, volume);
        }
    }
}
