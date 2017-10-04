using System.Collections.Generic;
using System.Linq;

namespace SoftFx.Common.Graphs
{
    public class Path<TNode, TEdge, TVal> where TNode : Node where TEdge : Edge<TNode>
    {
        public SparseGraph<TNode, TEdge> Graph { get; }

        public TVal Distance { get; }

        public TEdge[] PathEdges { get; }

        public TNode From { get; }

        public TNode To { get; }

        public TNode[] PathNodes { get; }

        public bool IsEmpty => PathEdges.Length == 0;

        public bool IsCycle => !IsEmpty && From.Id == To.Id;


        public Path(SparseGraph<TNode, TEdge> graph, TVal distance, IEnumerable<TEdge> pathEdges)
        {
            Graph = graph;
            Distance = distance;
            PathEdges = pathEdges.ToArray();
            if (!IsEmpty)
            {
                From = PathEdges[0].From;
                To = PathEdges[PathEdges.Length - 1].To;
                PathNodes = new TNode[PathEdges.Length + 1];
                PathNodes[0] = PathEdges[0].From;
                for (var i = 0; i < PathEdges.Length; i++)
                {
                    PathNodes[i + 1] = PathEdges[i].To;
                }
            }
        }


        public override string ToString()
        {
            return IsEmpty ? "<empty path>" : string.Join<TNode>(" - ", PathNodes);
        }
    }


    public class Path : Path<Node, Edge<Node>, double>
    {
        public Path(SparseGraph graph, double distance, IEnumerable<Edge<Node>> pathEdges) : base(graph, distance, pathEdges)
        {
        }
    }
}
