using System;

namespace SoftFx.Common.Graphs
{
    /// <summary>
    /// Represents basic graph node
    /// </summary>
    public class Node
    {
        public int Id { get; internal set; }

        public string Name { get; set; }


        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Name) ? Id.ToString() : Name;
        }
    }


    /// <summary>
    /// Represents basic weighted graph edge
    /// </summary>
    public class Edge<TNode> where TNode : Node
    {
        public TNode From { get; }

        public TNode To { get; }

        public virtual double Weight { get; set; }


        public Edge(TNode from, TNode to) : this(from, to, 1) { }

        public Edge(TNode from, TNode to, double weight)
        {
            From = from;
            To = to;
            Weight = weight;
        }


        public override string ToString()
        {
            return $"{From} - {To} = {Weight}";
        }
    }


    /// <summary>
    /// Common exception class for all graph errors for convinience
    /// </summary>
    public class GraphException : Exception
    {
        public GraphException(string msg) : base(msg) { }
    }
}
