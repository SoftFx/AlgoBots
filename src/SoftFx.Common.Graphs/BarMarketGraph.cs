using SoftFx.Common.BusinessObjects;
using SoftFx.Common.Extensions;
using System;
using System.Collections.Generic;
using TickTrader.Algo.Api;

namespace SoftFx.Common.Graphs
{
    /// <summary>
    /// Edge for market graph
    /// </summary>
    public class BarSymbolEdge : Edge<CurrencyNode>
    {
        public BarSymbol Symbol { get; }

        public OrderSide Side { get; }

        public double Commission { get; }

        public int PipsDigits { get; }

        public override double Weight
        {
            get
            {
                if (!Symbol.HasValidQuotes(Side))
                    return double.NaN;
                var price = Symbol.RoundPrice(Symbol.BestPrice(Side).ApplyCommission(Commission, Side), Side, PipsDigits);
                return (Side == OrderSide.Sell ? -1 : 1) * Math.Log(price);
            }
            set { base.Weight = value; }
        }

        public double ReverseWeight
        {
            get
            {
                var side = Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
                if (!Symbol.HasValidQuotes(side))
                    return double.NaN;
                var price = Symbol.RoundPrice(Symbol.BestPrice(side).ApplyCommission(Commission, side), side, PipsDigits);
                return (side == OrderSide.Sell ? -1 : 1) * Math.Log(price);
            }
        }


        public BarSymbolEdge(CurrencyNode from, CurrencyNode to, BarSymbol symbol) : this(from, to, symbol, 0) { }

        public BarSymbolEdge(CurrencyNode from, CurrencyNode to, BarSymbol symbol, double commission) : this(from, to, symbol, commission, 0) { }

        public BarSymbolEdge(CurrencyNode from, CurrencyNode to, BarSymbol symbol, double commission, int pipsDigits) : base(from, to)
        {
            if (symbol.BaseCurrency != from.Name && symbol.CounterCurrency != from.Name)
                throw new GraphException($"Node {from} doesn't correspond to symbol {symbol.Name}");
            if (symbol.BaseCurrency != to.Name && symbol.CounterCurrency != to.Name)
                throw new GraphException($"Node {to} doesn't correspond to symbol {symbol.Name}");
            if (from.Name == to.Name)
                throw new GraphException("Symbol edge can't be a loop");

            Symbol = symbol;
            Side = symbol.BaseCurrency == from.Name && symbol.CounterCurrency == to.Name ? OrderSide.Sell : OrderSide.Buy;
            Commission = commission;
            PipsDigits = pipsDigits;
        }


        public override string ToString()
        {
            return $"{From} - {To} = {Math.Exp((Side == OrderSide.Sell ? -1 : 1) * Weight)} ({Side} {Symbol.Name}, Commission = {Commission})";
        }
    }


    /// <summary>
    /// Graph to reflect historical best rates
    /// </summary>
    public class BarMarketGraph : SparseGraph<CurrencyNode, BarSymbolEdge>
    {
        private Dictionary<string, CurrencyNode> _currencyCache;


        /// <summary>
        /// Gets node by currency
        /// </summary>
        /// <returns>Null if there is no node with specified currency, otherwise will return requested node</returns>
        public CurrencyNode this[string currency]
        {
            get
            {
                if (_currencyCache.ContainsKey(currency))
                {
                    return _currencyCache[currency];
                }
                return null;
            }
        }


        public BarMarketGraph()
        {
            _currencyCache = new Dictionary<string, CurrencyNode>();
        }

        public BarMarketGraph(AlgoPlugin plugin) : this()
        {
            if (plugin == null)
                throw new ArgumentException("Plugin cannot be null");

            foreach (var currency in plugin.Currencies)
            {
                if (currency.IsNull)
                    continue;

                AddNode(new CurrencyNode(currency));
            }
        }


        public override void AddNode(CurrencyNode node)
        {
            base.AddNode(node);

            if (_currencyCache.ContainsKey(node.Name))
                throw new GraphException($"Node {node} is duplicate");

            _currencyCache.Add(node.Name, node);
        }


        /// <summary>
        /// Adds edge to market graph finding required currencies
        /// </summary>
        public void AddEdge(string fromCurrency, string toCurrency, BarSymbol symbol)
        {
            AddEdge(fromCurrency, toCurrency, symbol, 0);
        }

        /// <summary>
        /// Adds edge to market graph finding required currencies
        /// </summary>
        public void AddEdge(string fromCurrency, string toCurrency, BarSymbol symbol, double commission)
        {
            AddEdge(fromCurrency, toCurrency, symbol, commission, 2);
        }

        /// <summary>
        /// Adds edge to market graph finding required currencies
        /// </summary>
        public void AddEdge(string fromCurrency, string toCurrency, BarSymbol symbol, double commission, int pipsDigits)
        {
            if (!_currencyCache.ContainsKey(fromCurrency))
                throw new GraphException($"Node {fromCurrency} not found");
            if (!_currencyCache.ContainsKey(toCurrency))
                throw new GraphException($"Node {toCurrency} not found");

            AddEdge(new BarSymbolEdge(_currencyCache[fromCurrency], _currencyCache[toCurrency], symbol, commission, pipsDigits));
        }
    }
}
