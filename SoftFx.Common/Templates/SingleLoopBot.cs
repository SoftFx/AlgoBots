using System;
using System.Threading.Tasks;

namespace SoftFx.Routines
{
    /// <summary>
    /// Extends TradeBot with some common functionality, adds iteration routine
    /// </summary>
    public abstract class SingleLoopBot<TConfig> : TradeBotExtended<TConfig> where TConfig : class, IConfig, new()
    {
        /// <summary>
        /// Defines how long to wait between loop. Default is 100 ms
        /// </summary>
        public int LoopTimeout { get; set; }

        /// <summary>
        /// Defines whether bot will print it's config to status on each iteration
        /// </summary>
        public bool DisplayConfig { get; set; }

        /// <summary>
        /// Current iteration identifier. 0 means not started yet
        /// </summary>
        public ulong IterationId { get; protected set; }


        protected SingleLoopBot()
        {
            LoopTimeout = 100;
            DisplayConfig = true;
            IterationId = 0;
        }


        protected override void BotRoutine()
        {
            if (LoopTimeout <= 0)
                throw new ArgumentException($"Can't execute bot loop routine with timeout = {LoopTimeout}");

            BotLoopRoutine();
        }


        /// <summary>
        /// Execute common bot loop routine: call iteration and wait timeout
        /// </summary>
        protected virtual async void BotLoopRoutine()
        {
            try
            {
                while (!IsStopRequested)
                {
                    await RunIteration();
                    await Task.Delay(LoopTimeout, CancelToken);
                }
            }
            catch (TaskCanceledException) { PrintDebug("Bot loop routine was canceled"); }
            catch (OperationCanceledException) { PrintDebug("Bot loop routine was canceled"); }
        }

        /// <summary>
        /// Handles common iteration routine
        /// </summary>
        /// <returns></returns>
        protected virtual async Task RunIteration()
        {
            IterationId++;
            try
            {
                if (DisplayConfig && Config != null)
                {
                    Status.WriteLine(Config.ToString());
                    Status.WriteLine();
                }
                await Iteration();
            }
            catch (Exception ex)
            {
                PrintError($"Iteration {IterationId} failed: {ex}");
                Status.WriteLine("Iteration failed");
            }
            Status.Flush();
        }

        /// <summary>
        /// Called every <code>LoopTimeout</code> ms for executing iteration routine
        /// </summary>
        protected abstract Task Iteration();


        #region Utility methods

        /// <summary>
        /// Asyncronously waits for next iteration
        /// </summary>
        /// <returns>New iteration id</returns>
        public Task<ulong> WaitNextIteration()
        {
            return WaitNextIteration(IterationId);
        }

        /// <summary>
        /// Asyncronously waits for iteration with id > provided value
        /// </summary>
        /// <returns>New iteration id</returns>
        public async Task<ulong> WaitNextIteration(ulong currentIteration)
        {
            while (currentIteration >= IterationId && !IsStopRequested)
            {
                await Task.Delay(System.Math.Max(LoopTimeout / 5, 1), CancelToken);
            }
            return IterationId;
        }

        #endregion Utility methods
    }
}
