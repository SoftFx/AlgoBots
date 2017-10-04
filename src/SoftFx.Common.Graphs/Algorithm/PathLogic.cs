using System;
using System.Collections.Generic;
using TickTrader.Algo.Api.Math;

namespace SoftFx.Common.Graphs.Algorithm
{
    /// <summary>
    /// Wrapper for custom logic needed for path finding algorithms
    /// </summary>
    /// <typeparam name="TEdge"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    public class PathLogic<TEdge, TVal> : IComparer<TVal>
    {
        private Func<TEdge, TVal> _extractFunc;
        private Func<TVal, TVal, TVal> _attachFunc;
        private Func<TVal, TVal, TVal> _detachFunc;
        private Func<TVal, TVal, int> _compareFunc;


        public TVal UnreachableValue { get; }

        public TVal ZeroValue { get; }


        protected PathLogic(TVal unreachableValue, TVal zeroValue = default(TVal))
        {
            UnreachableValue = unreachableValue;
            ZeroValue = zeroValue;
        }

        /// <summary>
        /// Functions can be passed from outside or overloaded in child classes
        /// </summary>
        public PathLogic(Func<TEdge, TVal> extractFunc, Func<TVal, TVal, TVal> attachFunc, Func<TVal, TVal, TVal> detachFunc,
            Func<TVal, TVal, int> compareFunc, TVal unreachableValue, TVal zeroValue = default(TVal)) : this(unreachableValue, zeroValue)
        {
            _extractFunc = extractFunc;
            _attachFunc = attachFunc;
            _detachFunc = detachFunc;
            _compareFunc = compareFunc;
        }


        /// <summary>
        /// Calculates value equal to weight of provided edge
        /// </summary>
        public virtual TVal Extract(TEdge edge)
        {
            return _extractFunc(edge);
        }

        /// <summary>
        /// Performs operation equal to addition of two values
        /// </summary>
        public virtual TVal Attach(TVal x, TVal y)
        {
            return _attachFunc(x, y);
        }

        /// <summary>
        /// Performs operation equal to subtraction of two values
        /// </summary>
        public virtual TVal Detach(TVal x, TVal y)
        {
            return _detachFunc(x, y);
        }

        /// <summary>
        /// Compares to path values
        /// </summary>
        /// <returns>Less than 0 if x is lower than y, 0 if x equals y, greater than 0 if x is greater than y</returns>
        public virtual int Compare(TVal x, TVal y)
        {
            return _compareFunc(x, y);
        }

        /// <summary>
        /// Calculates new path value in case provided edge is added to it
        /// </summary>
        /// <param name="val">Current path value to edge from node</param>
        /// <param name="edge">Edge to be added to path</param>
        /// <returns>New path value</returns>
        public virtual TVal Relax(TVal val, TEdge edge)
        {
            return Attach(val, Extract(edge));
        }


        /// <summary>
        /// Determines if x is lower than y
        /// </summary>
        /// <returns>true if <code>Compare</code> function returns value less than zero, false otherwise</returns>
        public bool Lt(TVal x, TVal y)
        {
            return Compare(x, y) < 0;
        }

        /// <summary>
        /// Determines if x is lower than or equal to y
        /// </summary>
        /// <returns>true if <code>Compare</code> function returns value less than or equal to zero, false otherwise</returns>
        public bool Lte(TVal x, TVal y)
        {
            return Compare(x, y) <= 0;
        }

        /// <summary>
        /// Determines if x is greater than y
        /// </summary>
        /// <returns>true if <code>Compare</code> function returns value greater than zero, false otherwise</returns>
        public bool Gt(TVal x, TVal y)
        {
            return Compare(x, y) > 0;
        }

        /// <summary>
        /// Determines if x is greater than or equal to y
        /// </summary>
        /// <returns>true if <code>Compare</code> function returns value greater than or equal to zero, false otherwise</returns>
        public bool Gte(TVal x, TVal y)
        {
            return Compare(x, y) >= 0;
        }

        /// <summary>
        /// Determines if x is equal to y
        /// </summary>
        /// <returns>true if <code>Compare</code> function returns value equal to zero, false otherwise</returns>
        public bool E(TVal x, TVal y)
        {
            return Compare(x, y) == 0;
        }
    }


    public class PathLogic<TNode> : PathLogic<Edge<TNode>, double> where TNode : Node
    {
        public PathLogic(double unreachableValue = double.MaxValue) : base(unreachableValue) { }

        public override double Extract(Edge<TNode> edge)
        {
            return edge.Weight;
        }

        public override double Attach(double x, double y)
        {
            return x + y;
        }

        public override double Detach(double x, double y)
        {
            return x - y;
        }

        public override int Compare(double x, double y)
        {
            return x.Lt(y) ? -1 : (x.Gt(y) ? 1 : 0);
        }
    }


    public class PathLogic : PathLogic<Node> { }
}
