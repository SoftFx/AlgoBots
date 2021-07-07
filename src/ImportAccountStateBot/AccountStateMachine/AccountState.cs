using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImportAccountStateBot
{
    public sealed class AccountState
    {
        private readonly Dictionary<string, PositionState> _positions;
        private readonly string _stateToString;

        public PositionState this[string symbol]
        {
            get
            {
                PositionState state = null;
                _positions?.TryGetValue(symbol, out state);

                return state;
            }
        }


        public SortedSet<string> Symbols { get; }

        public DateTime StateTime { get; }

        public bool IsEmpty => _positions.Count == 0;


        public AccountState(DateTime stateTime, IEnumerable<PositionState> states)
        {
            _positions = states?.ToDictionary(k => k.Symbol, v => v);

            Symbols = new SortedSet<string>(_positions?.Keys);
            StateTime = stateTime;

            _stateToString = StateToString();
        }


        public List<TransactionToken> TokensToNextPositionsStates(AccountState nextState)
        {
            var tokens = new List<TransactionToken>(nextState.Symbols.Count);

            foreach (var symbol in Symbols.Union(nextState.Symbols))
            {
                var token = PositionState.GetConversionToken(this[symbol], nextState[symbol]);

                tokens.Add(token);
            }

            return tokens;
        }

        private string StateToString()
        {
            var sb = new StringBuilder(1 << 8);

            foreach (var state in _positions.Values)
                sb.AppendLine($"{state}");

            return sb.ToString();
        }

        public override string ToString() => _stateToString;
    }
}
