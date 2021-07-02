using ImportAccountStateBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TickTrader.Algo.Api;

namespace ImportAccountStateBot
{
    public sealed class AccountStateMachine
    {
        private readonly LinkedList<AccountState> _states;
        private readonly ITimeProvider _time;

        private AccountState _currentState;


        public AccountState CurrentState
        {
            get => _currentState;

            set
            {
                _currentState = value;

                if (value != null)
                    SendTokensToNextState();
            }
        }

        public AccountState ExpectedState => _states.First?.Value;

        public bool IsNextStateTime => ExpectedState != null && _time.UtcNow >= ExpectedState.StateTime;


        public Action<TransactionToken> PushToken;


        public AccountStateMachine(IEnumerable<AccountState> states, ITimeProvider time)
        {
            _states = new LinkedList<AccountState>(states);
            _time = time;
        }

        public void InitCurrentState(NetPositionList positions)
        {
            var currentTime = _time.UtcNow;
            var states = positions.Select(u => PositionState.ParsePosition(u, currentTime));

            RollToNextState();

            CurrentState = new AccountState(currentTime, states);
        }

        private void RollToNextState()
        {
            while (IsNextStateTime)
                _states.RemoveFirst();
        }

        public void ToNextAccountState()
        {
            if (IsNextStateTime)
            {
                var expected = ExpectedState;
                _states.RemoveFirst();

                CurrentState = expected;
            }
        }

        private void SendTokensToNextState()
        {
            if (ExpectedState == null)
                return;

            var tokens = _currentState.TokensToNextPositionsStates(ExpectedState);

            foreach (var token in tokens)
                if (!token.IsEmpty)
                    PushToken?.Invoke(token);
        }

        public string CurrentMachineStateString()
        {
            var sb = new StringBuilder(1 << 10);

            sb.AppendTimeToNextState(ExpectedState, _time);
            sb.AppendLine();

            sb.AppendAccountState(CurrentState, "Last");
            sb.AppendAccountState(ExpectedState, "Expected");

            return sb.ToString();
        }
    }
}
