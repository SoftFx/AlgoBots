namespace _100YearPortfolio
{
    internal sealed class RecalculationEvent
    {
        private DateTime _nextActivationTime;


        public Func<DateTime, DateTime> ChangeTimeAction { get; init; }

        public Func<Task> RecalculateAction { get; init; }


        public Task Recalculate(DateTime utcNow)
        {
            if (_nextActivationTime < utcNow)
            {
                _nextActivationTime = ChangeTimeAction(utcNow);

                return RecalculateAction();
            }

            return Task.CompletedTask;
        }

        public long GetLeftTime(DateTime utcNow) => (long)(_nextActivationTime - utcNow).TotalSeconds;
    }
}
