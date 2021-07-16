using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace SoftFx.Routines
{
    /// <summary>
    /// Possible bot states
    /// </summary>
    public enum BotState
    {
        Created,
        Initializing,
        Started,
        Failed,
        Stopping,
        Stopped,
        FatalError,
    }


    /// <summary>
    /// Extends TradeBot with some common functionality
    /// </summary>
    public abstract class TradeBotExtended : TradeBot
    {
        protected CancellationTokenSource _cancelTokenSrc;
        protected string _stopReason;


        /// <summary>
        /// Defines whether new funcionality should do debug actions like printing more output and etc
        /// </summary>
        public bool IsDebug { get; set; }

        /// <summary>
        /// True if OnStop has been called, false otherwise
        /// </summary>
        public bool IsStopRequested { get; protected set; }

        /// <summary>
        /// Bot loop routine cancelation token for awaitable operations
        /// </summary>
        public CancellationToken CancelToken { get; protected set; }

        /// <summary>
        /// Current bot state
        /// </summary>
        public BotState State { get; protected set; }


        protected TradeBotExtended()
        {
            IsDebug = false;
            IsStopRequested = false;
        }


        /// <summary>
        /// Aborts bot execution. Use to abort before bot routine has started
        /// </summary>
        public void Abort()
        {
            CancelBotRoutine();
            Exit();
        }

        /// <summary>
        /// Gently aborts bot execution. Use to abort after bot routine has started
        /// </summary>
        public async void PullHandbrake(string reason)
        {
            PrintError($"Handbreak pulled: {reason}");
            _stopReason = reason;
            await AsyncStop();
            Exit();
        }


        protected override async void OnStart()
        {
            if (!CancelBotRoutine())
                return;

            IsStopRequested = false;
            _cancelTokenSrc = new CancellationTokenSource();
            CancelToken = _cancelTokenSrc.Token;
            _stopReason = "";

            State = BotState.Initializing;
            Status.WriteLine("Initializing");
            Status.Flush();

            if (!InitBotConfig(out string error))
            {
                State = BotState.FatalError;
                _stopReason = $"Configuration failure{(error != null ? ": " : "")}{error}";
                Exit();
                return;
            }

            try
            {
                await InitInternal();
            }
            catch (Exception ex)
            {
                var errorMessage = ex is ValidationException ? ex.Message : ex.ToString();
                PrintError($"Failed to init bot: {errorMessage}");
                State = BotState.Failed;
                _stopReason = $"Init failed: {errorMessage}";
                Exit();
                return;
            }

            if (IsStopRequested)
                return;

            Print("Started");
            State = BotState.Started;
            Status.WriteLine("Started");
            Status.Flush();

            try
            {
                BotRoutine();
            }
            catch (Exception ex)
            {
                PrintError($"Bot routine execution failed: {ex}");
                State = BotState.Failed;
                _stopReason = $"Bot routine failed: {ex}";
                Abort();
                return;
            }
        }

        protected override async Task AsyncStop()
        {
            if (State != BotState.Failed && State != BotState.Started)
                return;

            CancelBotRoutine();

            State = BotState.Stopping;
            Status.Flush();
            Status.WriteLine("Stopping");

            try
            {
                await StopInternal();
            }
            catch (Exception ex)
            {
                PrintError($"Failed to stop bot gracefully: {ex}");
            }
        }

        protected override void OnStop()
        {
            if (State != BotState.Stopping)
            {
                CancelBotRoutine();
            }

            try
            {
                ExitInternal();
            }
            catch (Exception ex)
            {
                PrintError($"Failed to exit bot gracefully: {ex}");
            }

            State = BotState.Stopped;
            Status.Flush();
            Status.WriteLine("Stopped");
            if (!string.IsNullOrWhiteSpace(_stopReason))
            {
                Status.WriteLine(_stopReason);
            }
        }

        protected void SetStopReason(string reason)
        {
            _stopReason = reason;
        }

        /// <summary>
        /// Initializes bot config on start
        /// </summary>
        /// <returns>true if config is successfully initialized, false otherwise</returns>
        protected virtual bool InitBotConfig(out string error)
        {
            error = null;
            return true;
        }

        /// <summary>
        /// Executes bot routine
        /// </summary>
        protected virtual void BotRoutine() { }


        /// <summary>
        /// Cancels current bot routine
        /// </summary>
        /// <returns>true if cancel was successful, false otherwise</returns>
        protected virtual bool CancelBotRoutine()
        {
            IsStopRequested = true;
            if (_cancelTokenSrc != null)
            {
                try
                {
                    _cancelTokenSrc.Cancel();
                    _cancelTokenSrc = null;
                }
                catch (TaskCanceledException) { Print("Bot task was canceled"); }
                catch (OperationCanceledException) { Print("Bot operation was canceled"); }
                catch (Exception ex)
                {
                    PrintError($"Failed to cancel bot routine: {ex}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Called after reading bot configuration for initialization routine
        /// </summary>
        protected virtual Task InitInternal()
        {
            return Task.FromResult(this);
        }

        /// <summary>
        /// Used to gracefully stop all bot routines
        /// </summary>
        protected virtual Task StopInternal()
        {
            return Task.FromResult(this);
        }

        /// <summary>
        /// Called after bot stop or in case exit is called
        /// </summary>
        protected virtual void ExitInternal() { }


        #region IBotLogExtended implementation

        public void Print(IEnumerable<string> msgs)
        {
            foreach (var msg in msgs)
            {
                Print(msg);
            }
        }

        public void PrintMany(params string[] msgs)
        {
            foreach (var msg in msgs)
            {
                Print(msg);
            }
        }

        public void PrintError(IEnumerable<string> msgs)
        {
            foreach (var msg in msgs)
            {
                Print(msg);
            }
        }

        public void PrintErrorMany(params string[] msgs)
        {
            foreach (var msg in msgs)
            {
                Print(msg);
            }
        }

        public void PrintDebug(string msg)
        {
            if (IsDebug)
            {
                Print(msg);
            }
        }

        public void PrintDebug(IEnumerable<string> msgs)
        {
            if (IsDebug)
            {
                foreach (var msg in msgs)
                {
                    Print(msg);
                }
            }
        }

        public void PrintDebug(params string[] msgs)
        {
            if (IsDebug)
            {
                foreach (var msg in msgs)
                {
                    Print(msg);
                }
            }
        }

        public void Say(string msg)
        {
            Print(msg);
            Status.WriteLine(msg);
        }

        public void Say(IEnumerable<string> msgs)
        {
            foreach (var msg in msgs)
            {
                Print(msg);
                Status.WriteLine(msg);
            }
        }

        public void Say(params string[] msgs)
        {
            foreach (var msg in msgs)
            {
                Print(msg);
                Status.WriteLine(msg);
            }
        }

        public void SayDebug(string msg)
        {
            Print(msg);
            if (IsDebug)
            {
                Status.WriteLine(msg);
            }
        }

        public void SayDebug(IEnumerable<string> msgs)
        {
            foreach (var msg in msgs)
            {
                Print(msg);
                if (IsDebug)
                {
                    Status.WriteLine(msg);
                }
            }
        }

        public void SayDebug(params string[] msgs)
        {
            foreach (var msg in msgs)
            {
                Print(msg);
                if (IsDebug)
                {
                    Status.WriteLine(msg);
                }
            }
        }

        public void SayError(string msg)
        {
            PrintError(msg);
            Status.WriteLine($"Error: {msg}");
        }

        public void SayError(IEnumerable<string> msgs)
        {
            foreach (var msg in msgs)
            {
                PrintError(msg);
                Status.WriteLine($"Error: {msg}");
            }
        }

        public void SayError(params string[] msgs)
        {
            foreach (var msg in msgs)
            {
                PrintError(msg);
                Status.WriteLine($"Error: {msg}");
            }
        }

        public void SayErrorDebug(string msg)
        {
            PrintError(msg);
            if (IsDebug)
            {
                Status.WriteLine($"Error: {msg}");
            }
        }

        public void SayErrorDebug(IEnumerable<string> msgs)
        {
            foreach (var msg in msgs)
            {
                PrintError(msg);
                if (IsDebug)
                {
                    Status.WriteLine($"Error: {msg}");
                }
            }
        }

        public void SayErrorDebug(params string[] msgs)
        {
            foreach (var msg in msgs)
            {
                PrintError(msg);
                if (IsDebug)
                {
                    Status.WriteLine($"Error: {msg}");
                }
            }
        }

        #endregion IBotLogExtended implementation


        #region Trade methods

        /// <summary>
        /// Opens market order
        /// </summary>
        /// <returns>Order execution result</returns>
        public OrderCmdResult OpenMarket(string symbol, OrderSide side, double volume, double price,
            double? sl = default(double?), double? tp = default(double?), string comment = "", string tag = null)
        {
            return OpenOrder(symbol, OrderType.Market, side, volume, null, price, null, sl, tp, comment, OrderExecOptions.None, tag);
        }

        /// <summary>
        /// Opens market order
        /// </summary>
        /// <returns>Order execution result</returns>
        public Task<OrderCmdResult> OpenMarketAsync(string symbol, OrderSide side, double volume, double price,
            double? sl = default(double?), double? tp = default(double?), string comment = "", string tag = null)
        {
            return OpenOrderAsync(symbol, OrderType.Market, side, volume, null, price, null, sl, tp, comment, OrderExecOptions.None, tag);
        }

        /// <summary>
        /// Opens limit order
        /// </summary>
        /// <returns>Order execution result</returns>
        public OrderCmdResult OpenLimit(string symbol, OrderSide side, double volume, double price,
            double? sl = default(double?), double? tp = default(double?), string comment = "", string tag = null)
        {
            return OpenOrder(symbol, OrderType.Limit, side, volume, null, price, null, sl, tp, comment, OrderExecOptions.None, tag);
        }

        /// <summary>
        /// Opens limit order
        /// </summary>
        /// <returns>Order execution result</returns>
        public Task<OrderCmdResult> OpenLimitAsync(string symbol, OrderSide side, double volume, double price,
            double? sl = default(double?), double? tp = default(double?), string comment = "", string tag = null)
        {
            return OpenOrderAsync(symbol, OrderType.Limit, side, volume, null, price, null, sl, tp, comment, OrderExecOptions.None, tag);
        }

        /// <summary>
        /// Tries to cancel orders an <code>attemptsCnt</code> number of times
        /// </summary>
        /// <param name="orderId">Order Id to cancel</param>
        /// <param name="attemptsCnt">Max number of retries, -1 for infinitive attempts</param>
        /// <returns></returns>
        public async Task CancelOrder(string orderId, int attemptsCnt)
        {
            var exists = true;
            for (; exists && attemptsCnt != 0; attemptsCnt--)
            {
                var res = await CancelOrderAsync(orderId);
                if (res.ResultCode == OrderCmdResultCodes.Ok || res.ResultCode == OrderCmdResultCodes.OrderNotFound)
                {
                    exists = false;
                }
            }
        }

        /// <summary>
        /// Cancel all existing orders of the specified type with current bot instance id
        /// </summary>
        public async Task CancelOrders(OrderType type)
        {
            var ordersToCancel = FindOrders(type);
            foreach (var order in ordersToCancel)
                CancelOrderInternal(order.Id, -1);

            while (CountOrders(type) > 0)
            {
                // Because average response time from server 100-200 ms
                await Task.Delay(100);
            }
        }

        /// <summary>
        /// Cancel all existing orders of the specified symbol and type with current bot instance id
        /// </summary>
        public async Task CancelOrders(string symbol, OrderType type)
        {
            var ordersToCancel = FindOrders(symbol, type);
            foreach (var order in ordersToCancel)
                CancelOrderInternal(order.Id, -1);

            while (CountOrders(symbol, type) > 0)
            {
                // Because average response time from server 100-200 ms
                await Task.Delay(100);
            }
        }

        /// <summary>
        /// Cancel all existing limit orders with current bot instance id
        /// </summary>
        public Task CancelLimits()
        {
            return CancelOrders(OrderType.Limit);
        }

        /// <summary>
        /// Cancel all existing limit orders of the specified symbol with current bot instance id
        /// </summary>
        public Task CancelLimits(string symbol)
        {
            return CancelOrders(symbol, OrderType.Limit);
        }


        private async void CancelOrderInternal(string orderId, int attemptsCnt)
        {
            await CancelOrder(orderId, attemptsCnt);
        }

        #endregion Trade methods


        #region Search methods

        /// <summary>
        /// Finds first symbol in symbol lists that has specified currencies
        /// </summary>
        /// <returns>Symbol if it exists, null otherwise</returns>
        public Symbol FindSymbol(string baseCurrency, string counterCurrency)
        {
            return Symbols.FirstOrDefault(s => s.BaseCurrency == baseCurrency && s.CounterCurrency == counterCurrency);
        }

        /// <summary>
        /// Finds symbols that have specified currencies
        /// </summary>
        /// <returns>Symbols that match criteria</returns>
        public List<Symbol> FindSymbols(string baseCurrency, string counterCurrency)
        {
            return Symbols.Where(s => s.BaseCurrency == baseCurrency && s.CounterCurrency == counterCurrency).ToList();
        }

        /// <summary>
        /// Finds first tradable symbol in symbol lists that has specified currencies
        /// </summary>
        /// <returns>Symbol if it exists, null otherwise</returns>
        public Symbol FindTradableSymbol(string currency1, string currency2, bool? direction)
        {
            return Symbols.FirstOrDefault(s => !s.IsNull && s.IsTradeAllowed && !s.Name.EndsWith("_L")
                && (((direction ?? true) && s.BaseCurrency == currency1 && s.CounterCurrency == currency2)
                || ((!direction ?? true) && s.BaseCurrency == currency2 && s.CounterCurrency == currency1)));
        }

        /// <summary>
        /// Finds tradable symbols that have specified currencies
        /// </summary>
        /// <returns>Symbols that match criteria</returns>
        public List<Symbol> FindTradableSymbols(string currency1, string currency2, bool? direction)
        {
            return Symbols.Where(s => !s.IsNull && s.IsTradeAllowed && !s.Name.EndsWith("_L")
                && (((direction ?? true) && s.BaseCurrency == currency1 && s.CounterCurrency == currency2)
                || ((!direction ?? true) && s.BaseCurrency == currency2 && s.CounterCurrency == currency1))).ToList();
        }

        /// <summary>
        /// Finds first order with current bot instance id
        /// </summary>
        /// <returns>Order if exists, otherwise null</returns>
        public Order FindOrder()
        {
            return Account.Orders.FirstOrDefault(o => o.InstanceId == Id);
        }

        /// <summary>
        /// Finds first order with specified tag and current bot instance id
        /// </summary>
        /// <returns>Order if exists, otherwise null</returns>
        public Order FindOrder(string tag)
        {
            return Account.Orders.FirstOrDefault(o => o.Tag == tag && o.InstanceId == Id);
        }

        /// <summary>
        /// Finds orders with current bot instance id
        /// </summary>
        /// <returns>Existing orders that match criteria</returns>
        public List<Order> FindOrders()
        {
            return Account.Orders.Where(o => o.InstanceId == Id).ToList();
        }

        /// <summary>
        /// Finds orders with specified tag and current bot instance id
        /// </summary>
        /// <returns>Existing orders that match criteria</returns>
        public List<Order> FindOrders(string tag)
        {
            return Account.Orders.Where(o => o.Tag == tag && o.InstanceId == Id).ToList();
        }

        /// <summary>
        /// Finds first order of the specified type with current bot instance id
        /// </summary>
        /// <returns>Order if exists, otherwise null</returns>
        public Order FindOrder(OrderType type)
        {
            return Account.Orders.FirstOrDefault(o => o.Type == type && o.InstanceId == Id);
        }

        /// <summary>
        /// Finds first order of the specified type with specified tag and current bot instance id
        /// </summary>
        /// <returns>Order if exists, otherwise null</returns>
        public Order FindOrder(OrderType type, string tag)
        {
            return Account.Orders.FirstOrDefault(o => o.Type == type && o.Tag == tag && o.InstanceId == Id);
        }

        /// <summary>
        /// Finds orders of the specified type with current bot instance id
        /// </summary>
        /// <returns>Existing orders that match criteria</returns>
        public List<Order> FindOrders(OrderType type)
        {
            return Account.Orders.Where(o => o.Type == type && o.InstanceId == Id).ToList();
        }

        /// <summary>
        /// Finds orders of the specified type with specified tag and current bot instance id
        /// </summary>
        /// <returns>Existing orders that match criteria</returns>
        public List<Order> FindOrders(OrderType type, string tag)
        {
            return Account.Orders.Where(o => o.Type == type && o.Tag == tag && o.InstanceId == Id).ToList();
        }

        /// <summary>
        /// Finds first order of the specified type and side with current bot instance id
        /// </summary>
        /// <returns>Order if exists, otherwise null</returns>
        public Order FindOrder(OrderType type, OrderSide side)
        {
            return Account.Orders.FirstOrDefault(o => o.Type == type && o.Side == side && o.InstanceId == Id);
        }

        /// <summary>
        /// Finds first order of the specified type and side with specified tag and current bot instance id
        /// </summary>
        /// <returns>Order if exists, otherwise null</returns>
        public Order FindOrder(OrderType type, OrderSide side, string tag)
        {
            return Account.Orders.FirstOrDefault(o => o.Type == type && o.Side == side && o.Tag == tag && o.InstanceId == Id);
        }

        /// <summary>
        /// Finds orders of the specified type and side with current bot instance id
        /// </summary>
        /// <returns>Existing orders that match criteria</returns>
        public List<Order> FindOrders(OrderType type, OrderSide side)
        {
            return Account.Orders.Where(o => o.Type == type && o.Side == side && o.InstanceId == Id).ToList();
        }

        /// <summary>
        /// Finds orders of the specified type and side with specified tag and current bot instance id
        /// </summary>
        /// <returns>Existing orders that match criteria</returns>
        public List<Order> FindOrders(OrderType type, OrderSide side, string tag)
        {
            return Account.Orders.Where(o => o.Type == type && o.Side == side && o.Tag == tag && o.InstanceId == Id).ToList();
        }

        /// <summary>
        /// Finds first order of the specified symbol and type with current bot instance id
        /// </summary>
        /// <returns>Order if exists, otherwise null</returns>
        public Order FindOrder(string symbol, OrderType type)
        {
            return Account.Orders.FirstOrDefault(o => o.Symbol == symbol && o.Type == type && o.InstanceId == Id);
        }

        /// <summary>
        /// Finds first order of the specified symbol and type with specified tag and current bot instance id
        /// </summary>
        /// <returns>Order if exists, otherwise null</returns>
        public Order FindOrder(string symbol, OrderType type, string tag)
        {
            return Account.Orders.FirstOrDefault(o => o.Symbol == symbol && o.Type == type && o.Tag == tag && o.InstanceId == Id);
        }

        /// <summary>
        /// Finds orders of the specified symbol and type with current bot instance id.
        /// If symbol is null or empty then it is skipped.
        /// </summary>
        /// <returns>Existing orders that match criteria</returns>
        public List<Order> FindOrders(string symbol, OrderType type)
        {
            return string.IsNullOrEmpty(symbol)
                ? FindOrders(type)
                : Account.Orders.Where(o => o.Symbol == symbol && o.Type == type && o.InstanceId == Id).ToList();
        }

        /// <summary>
        /// Finds orders of the specified symbol and type with specified tag and current bot instance id.
        /// If symbol is null or empty then it is skipped.
        /// </summary>
        /// <returns>Existing orders that match criteria</returns>
        public List<Order> FindOrders(string symbol, OrderType type, string tag)
        {
            return string.IsNullOrEmpty(symbol)
                ? FindOrders(type, tag)
                : Account.Orders.Where(o => o.Symbol == symbol && o.Type == type && o.Tag == tag && o.InstanceId == Id).ToList();
        }

        /// <summary>
        /// Finds first order of the specified symbol, type and side with current bot instance id
        /// </summary>
        /// <returns>Order if exists, otherwise null</returns>
        public Order FindOrder(string symbol, OrderType type, OrderSide side)
        {
            return Account.Orders.FirstOrDefault(o => o.Symbol == symbol && o.Type == type && o.Side == side && o.InstanceId == Id);
        }

        /// <summary>
        /// Finds first order of the specified symbol, type and side with specified tag and current bot instance id
        /// </summary>
        /// <returns>Order if exists, otherwise null</returns>
        public Order FindOrder(string symbol, OrderType type, OrderSide side, string tag)
        {
            return Account.Orders.FirstOrDefault(o => o.Symbol == symbol && o.Type == type && o.Side == side && o.Tag == tag && o.InstanceId == Id);
        }

        /// <summary>
        /// Finds orders of the specified symbol, type and side with current bot instance id.
        /// If symbol is null or empty then it is skipped.
        /// </summary>
        /// <returns>Existing orders that match criteria</returns>
        public List<Order> FindOrders(string symbol, OrderType type, OrderSide side)
        {
            return string.IsNullOrEmpty(symbol)
                ? FindOrders(type)
                : Account.Orders.Where(o => o.Symbol == symbol && o.Type == type && o.Side == side && o.InstanceId == Id).ToList();
        }

        /// <summary>
        /// Finds orders of the specified symbol, type and side with specified tag and current bot instance id.
        /// If symbol is null or empty then it is skipped.
        /// </summary>
        /// <returns>Existing orders that match criteria</returns>
        public List<Order> FindOrders(string symbol, OrderType type, OrderSide side, string tag)
        {
            return string.IsNullOrEmpty(symbol)
                ? FindOrders(type, tag)
                : Account.Orders.Where(o => o.Symbol == symbol && o.Type == type && o.Side == side && o.Tag == tag && o.InstanceId == Id).ToList();
        }

        /// <summary>
        /// Counts orders with current bot instance id
        /// </summary>
        /// <returns>Number of existing orders that match criteria</returns>
        public int CountOrders()
        {
            return Account.Orders.Count(o => o.InstanceId == Id);
        }

        /// <summary>
        /// Counts orders with specified tag and current bot instance id
        /// </summary>
        /// <returns>Number of existing orders that match criteria</returns>
        public int CountOrders(string tag)
        {
            return Account.Orders.Count(o => o.Tag == tag && o.InstanceId == Id);
        }

        /// <summary>
        /// Counts orders of the specified type with current bot instance id
        /// </summary>
        /// <returns>Number of existing orders that match criteria</returns>
        public int CountOrders(OrderType type)
        {
            return Account.Orders.Count(o => o.Type == type && o.InstanceId == Id);
        }

        /// <summary>
        /// Counts orders of the specified type with specified tag and current bot instance id
        /// </summary>
        /// <returns>Number of existing orders that match criteria</returns>
        public int CountOrders(OrderType type, string tag)
        {
            return Account.Orders.Count(o => o.Type == type && o.Tag == tag && o.InstanceId == Id);
        }

        /// <summary>
        /// Counts orders of the specified type and side with current bot instance id
        /// </summary>
        /// <returns>Number of existing orders that match criteria</returns>
        public int CountOrders(OrderType type, OrderSide side)
        {
            return Account.Orders.Count(o => o.Type == type && o.Side == side && o.InstanceId == Id);
        }

        /// <summary>
        /// Counts orders of the specified type and side with specified tag and current bot instance id
        /// </summary>
        /// <returns>Number of existing orders that match criteria</returns>
        public int CountOrders(OrderType type, OrderSide side, string tag)
        {
            return Account.Orders.Count(o => o.Type == type && o.Side == side && o.Tag == tag && o.InstanceId == Id);
        }

        /// <summary>
        /// Counts orders of the specified symbol and type with current bot instance id.
        /// If symbol is null or empty then it is skipped.
        /// </summary>
        /// <returns>Number of existing orders that match criteria</returns>
        public int CountOrders(string symbol, OrderType type)
        {
            return string.IsNullOrEmpty(symbol)
                ? CountOrders(type)
                : Account.Orders.Count(o => o.Symbol == symbol && o.Type == type && o.InstanceId == Id);
        }

        /// <summary>
        /// Counts orders of the specified symbol and type with specified tag and current bot instance id.
        /// If symbol is null or empty then it is skipped.
        /// </summary>
        /// <returns>Number of existing orders that match criteria</returns>
        public int CountOrders(string symbol, OrderType type, string tag)
        {
            return string.IsNullOrEmpty(symbol)
                ? CountOrders(type, tag)
                : Account.Orders.Count(o => o.Symbol == symbol && o.Type == type && o.Tag == tag && o.InstanceId == Id);
        }

        /// <summary>
        /// Counts orders of the specified symbol, type and side with current bot instance id.
        /// If symbol is null or empty then it is skipped.
        /// </summary>
        /// <returns>Number of existing orders that match criteria</returns>
        public int CountOrders(string symbol, OrderType type, OrderSide side)
        {
            return string.IsNullOrEmpty(symbol)
                ? CountOrders(type, side)
                : Account.Orders.Count(o => o.Symbol == symbol && o.Type == type && o.Side == side && o.InstanceId == Id);
        }

        /// <summary>
        /// Counts orders of the specified symbol, type and side with specified tag and current bot instance id.
        /// If symbol is null or empty then it is skipped.
        /// </summary>
        /// <returns>Number of existing orders that match criteria</returns>
        public int CountOrders(string symbol, OrderType type, OrderSide side, string tag)
        {
            return string.IsNullOrEmpty(symbol)
                ? CountOrders(type, side, tag)
                : Account.Orders.Count(o => o.Symbol == symbol && o.Type == type && o.Side == side && o.Tag == tag && o.InstanceId == Id);
        }

        #endregion Search methods


        #region Utility methods

        /// <summary>
        /// Get Symbol object for the specified order
        /// </summary>
        public Symbol GetOrderSymbol(Order order)
        {
            var symbol = Symbols[order.Symbol];
            if (symbol.IsNull)
            {
                PrintError($"Unknown symbol {order.Symbol} for order {order.Id}");
                if (IsDebug)
                {
                    Status.WriteLine($"Unknown symbol {order.Symbol} for order {order.Id}");
                }
            }
            return symbol;
        }

        /// <summary>
        /// Makes order string of standard format.
        /// </summary>
        /// <returns>Formatted string</returns>
        public string GetOrderString(string symbol, OrderSide side, double price, double lots)
        {
            return $"{side} {symbol} {lots} lots at {price}";
        }

        /// <summary>
        /// Makes order string of standard format.
        /// </summary>
        /// <returns>Formatted string</returns>
        public string GetOrderString(string symbol, OrderSide side, OrderType type, double price, double lots)
        {
            return $"{side} {type} {symbol} {lots} lots at {price}";
        }

        /// <summary>
        /// Makes Limit order string of standard format.
        /// </summary>
        /// <returns>Formatted string</returns>
        public string GetLimitString(string symbol, OrderSide side, double price, double lots)
        {
            return $"{side} Limit {symbol} {lots} lots at {price}";
        }

        /// <summary>
        /// Makes IoC order string of standard format.
        /// </summary>
        /// <returns>Formatted string</returns>
        public string GetIoCString(string symbol, OrderSide side, double price, double lots)
        {
            return $"{side} IoC {symbol} {lots} lots at {price}";
        }

        /// <summary>
        /// Determines whether cash account has enought money to open order
        /// </summary>
        /// <returns>true if there is enough money, false otherwise</returns>
        public bool EnoughMoneyFor(string symbol, OrderSide side, double price, double lots)
        {
            if (Account.Type != AccountTypes.Cash)
            {
                return false;
            }

            var s = Symbols[symbol];
            if (s.IsNull)
                throw new ArgumentException($"Symbol '{symbol}' not found");

            Asset asset;
            switch (side)
            {
                case OrderSide.Buy:
                    asset = Account.Assets[s.CounterCurrency];
                    if (asset == null)
                        throw new ArgumentException($"Asset '{s.CounterCurrency}' not found");
                    return asset.FreeVolume.Gte(price * lots * s.ContractSize);
                case OrderSide.Sell:
                    asset = Account.Assets[s.BaseCurrency];
                    if (asset == null)
                        throw new ArgumentException($"Asset '{s.BaseCurrency}' not found");
                    return asset.FreeVolume.Gte(lots * s.ContractSize);
                default:
                    throw new ArgumentException($"Unknown side '{side}'");
            }
        }

        #endregion Utility methods
    }


    /// <summary>
    /// Extends Tradebot with some common functionality, adds iteration routine and TOML configuration routine
    /// </summary>
    /// <typeparam name="TConfig">Configuration type which should be TOML compatible</typeparam>
    public abstract class TradeBotExtended<TConfig> : TradeBotExtended where TConfig : class, IConfig, new()
    {
        private TConfig _config;

        /// <summary>
        /// Configuration object
        /// </summary>
        public TConfig Config
        {
            get => _config;
            set
            {
                if (value == _config)
                    return;

                UnsubscribeConfigToLog(_config);

                _config = value;

                SubscribeConfigToLog(_config);
            }
        }


        protected TradeBotExtended()
        {
            Config = default;
        }


        /// <summary>
        /// Used to get config file full path for initalizing and reading bot configuration.
        /// </summary>
        /// <returns>Configuration file full path</returns>
        protected abstract string GetConfigFullPath();

        /// <summary>
        /// Used to get default config file name for creating sample config file in data folder
        /// </summary>
        /// <returns>Default configuration file name</returns>
        protected abstract string GetDefaultConfigFileName();

        /// <summary>
        /// Initializes and reads config on bot start
        /// </summary>
        protected override bool InitBotConfig(out string error)
        {
            error = null;

            try
            {
                EnsureConfiguration(System.IO.Path.Combine(Enviroment.BotDataFolder, $"{GetDefaultConfigFileName()}_sample"), true);

                var configPath = GetConfigFullPath();
                EnsureConfiguration(configPath, false);
                ReadConfiguration(configPath);
            }
            catch (Exception ex)
            {
                PrintError($"Failed to read bot config: {ex.Message}");
                return false;
            }

            try
            {
                Config.Init();
                Say(Config.ToString());
            }
            catch (Exception ex)
            {
                error = ex is ValidationException ? ex.Message : null;
                PrintError($"Failed to init bot config: {(ex is ValidationException ? error : ex.ToString())}");
                return false;
            }

            return true;
        }


        #region Configuration methods

        /// <summary>
        /// Ensure that provided file exists, or creates it with default configuration.
        /// Also always creates another file in bot data folder with name equal to <code>defaultFileName + "_sample"</code> that contains default configuration.
        /// </summary>
        public void EnsureConfiguration(string configFullPath, bool overwrite)
        {
            if (overwrite || !System.IO.File.Exists(configFullPath))
                SaveConfiguration(new TConfig(), configFullPath);
        }

        /// <summary>
        /// Reads configuration from provided file.
        /// </summary>
        public void ReadConfiguration(string configFullPath)
        {
            Config = Nett.Toml.ReadFile<TConfig>(configFullPath);
        }

        /// <summary>
        /// Saves provided configuration in specified file
        /// </summary>
        public void SaveConfiguration(TConfig config, string configFullPath)
        {
            Nett.Toml.WriteFile(config, configFullPath);
        }

        #endregion Configuration methods

        private void SubscribeConfigToLog(TConfig config)
        {
            if (config is BotConfig botConfig)
            {
                botConfig.PrintErrorEvent += PrintErrorToLog;
                botConfig.PrintWarningEvent += PrintToLog;
            }
        }

        private void UnsubscribeConfigToLog(TConfig config)
        {
            if (config is BotConfig botConfig)
            {
                botConfig.PrintErrorEvent -= PrintErrorToLog;
                botConfig.PrintWarningEvent -= PrintToLog;
            }
        }

        private void PrintErrorToLog(string msg) => PrintError(msg);

        private void PrintToLog(string msg) => Print(msg);
    }
}