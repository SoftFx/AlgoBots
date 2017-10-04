using System.Collections.Generic;
using System.Linq;

namespace SoftFx.Common.Graphs.Algorithm
{
    public class PathSearchResult<TNode, TEdge, TVal> where TNode : Node where TEdge : Edge<TNode>
    {
        public SparseGraph<TNode, TEdge> Graph { get; }

        public TNode From { get; }

        public TVal[] Distance { get; }

        public TEdge[] Ancestors { get; }

        public bool? HasNegativeCycle { get; internal set; }


        public PathSearchResult(SparseGraph<TNode, TEdge> graph, int fromId)
        {
            Graph = graph;
            From = graph[fromId];
            Distance = new TVal[Graph.NodesCnt];
            Ancestors = new TEdge[Graph.NodesCnt];
            HasNegativeCycle = null;
        }


        public override string ToString()
        {
            return $"Path search result for graph '{Graph.Name}' from node {From}";
        }


        public Path<TNode, TEdge, TVal> GetPath(int toId)
        {
            var pathEdges = new List<TEdge>();
            for (var nodeId = toId; nodeId != From.Id && Ancestors[nodeId] != null; nodeId = Ancestors[nodeId].From.Id)
            {
                pathEdges.Add(Ancestors[nodeId]);
            }
            pathEdges.Reverse();

            return pathEdges.FirstOrDefault()?.From.Id != From.Id
                ? new Path<TNode, TEdge, TVal>(Graph, Distance[toId], Enumerable.Empty<TEdge>())
                : new Path<TNode, TEdge, TVal>(Graph, Distance[toId], pathEdges);
        }

        /// <summary>
        /// Checks if dstNode is accessible via ancestors from srcNode
        /// </summary>
        public bool IsAncestor(int srcNode, int dstNode)
        {
            var nodeId = srcNode;
            for (; nodeId != dstNode && Ancestors[nodeId] != null; nodeId = Ancestors[nodeId].From.Id) ;
            return nodeId == dstNode;
        }
    }


    public class PathSearchResult : PathSearchResult<Node, Edge<Node>, double>
    {
        public PathSearchResult(SparseGraph graph, int formId) : base(graph, formId)
        {
        }
    }
}
