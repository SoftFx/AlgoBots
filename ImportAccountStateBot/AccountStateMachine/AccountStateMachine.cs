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
                if (_currentState != null)
                    SendTokensToNextState(value);

                _currentState = value;
            }
        }

        public AccountState ExpectedState => _states.First?.Value;

        public bool IsNextStateTime => ExpectedState != null && _time.UtcNow >= ExpectedState.StateTime;

        public bool IsFutureStateTime(AccountState state) => ExpectedState == null || ExpectedState.StateTime < state.StateTime;


        public Action<TransactionToken> PushToken;


        public AccountStateMachine(ITimeProvider time)
        {
            _states = new LinkedList<AccountState>();
            _time = time;
        }

        public void InitCurrentState(NetPositionList positions)
        {
            var currentTime = _time.UtcNow;
            var states = positions.Select(u => PositionState.ParsePosition(u, currentTime));

            var previousState = GetCurrentStatePosition() ?? _currentState;

            _currentState = null; //reset old current acc state (for Account reconnect)
            CurrentState = new AccountState(currentTime, states.ToList()); // set current account state

            if (previousState != null)
                CurrentState = previousState; // roll back to the previous state in the file
        }

        public void AddAccountStates(IEnumerable<AccountState> states)
        {
            foreach (var state in states)
                if (IsFutureStateTime(state))
                    _states.AddLast(state);
        }

        private AccountState GetCurrentStatePosition()
        {
            AccountState _previousState = null;

            while (IsNextStateTime)
            {
                _previousState = _states.First.Value;
                _states.RemoveFirst();
            }

            return _previousState;
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

        private void SendTokensToNextState(AccountState nextState)
        {
            if (nextState == null)
                return;

            var tokens = _currentState.TokensToNextPositionsStates(nextState);

            foreach (var token in tokens)
                if (!token.IsEmpty)
                    PushToken?.Invoke(token);
        }

        public string CurrentMachineStateString()
        {
            var sb = new StringBuilder(1 << 10);

            sb.AppendTimeToNextState(ExpectedState, _time);
            sb.AppendLine();

            sb.AppendAccountState(CurrentState, "Current");
            sb.AppendAccountState(ExpectedState, "Expected");

            return sb.ToString();
        }
    }
}
