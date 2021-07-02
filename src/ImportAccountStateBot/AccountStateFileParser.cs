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
        private readonly string[] _separator;

        private readonly bool _setEmptyState;


        public AccountStateFileParser(CSVFileConfig config, bool setEmptyState)
        {
            _separator = new string[] { config.Separator };

            _setEmptyState = setEmptyState;
            _config = config;
        }


        public AccountStateMachine ReadAccountStates(string file, ITimeProvider provider)
        {
            var states = ReadPositionsStates(file);
            var stateGroups = states.OrderBy(u => u.Time).ThenBy(u => u.Symbol)
                                    .GroupBy(u => u.Time).Select(u => new AccountState(u.Key, u)).ToList();

            if (_setEmptyState)
            {
                var lastStateTime = stateGroups.LastOrDefault()?.StateTime ?? provider.UtcNow;

                if (lastStateTime < provider.UtcNow)
                    lastStateTime = provider.UtcNow.AddMinutes(5);

                stateGroups.Add(new AccountState(lastStateTime, new List<PositionState>()));
            }

            return new AccountStateMachine(stateGroups, provider);
        }

        private List<PositionState> ReadPositionsStates(string file)
        {
            var states = new List<PositionState>(1 << 5);

            using (var fs = new FileStream(file, FileMode.Open))
            {
                using (var sr = new StreamReader(fs))
                {
                    if (_config.SkipFirstLine) //skip .csv file headers
                        sr.ReadLine();

                    while (!sr.EndOfStream)
                        states.Add(ParseStringToPosition(sr.ReadLine()));
                }
            }

            return states;
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
