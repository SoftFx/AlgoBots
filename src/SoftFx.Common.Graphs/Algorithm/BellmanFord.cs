using System.Collections.Generic;

namespace SoftFx.Common.Graphs.Algorithm
{
    public static class BellmanFord<TNode, TEdge> where TNode : Node where TEdge : Edge<TNode>
    {
        /// <summary>
        /// Calculates shortest paths from start node to all other nodes in graph
        /// </summary>
        /// <returns>Array of shortest path from start node to all others</returns>
        public static PathSearchResult<TNode, TEdge, TVal> CalculateShortestPaths<TVal>(SparseGraph<TNode, TEdge> graph,
            PathLogic<TEdge, TVal> pathLogic, int startId)
        {
            var res = new PathSearchResult<TNode, TEdge, TVal>(graph, startId);
            var n = graph.NodesCnt;
            for (var i = 0; i < n; i++)
            {
                res.Distance[i] = pathLogic.UnreachableValue;
            }

            res.Distance[startId] = pathLogic.ZeroValue;
            for (var i = 0; i < n; i++)
            {
                if (!ExecutePhase(graph, pathLogic, res))
                    break;
            }

            res.HasNegativeCycle = ExecutePhase(graph, pathLogic, res);

            return res;
        }

        /// <summary>
        /// Calculates shortest paths from start node to all other nodes in graph
        /// </summary>
        /// <returns>Array of shortest path from start node to all others</returns>
        public static PathSearchResult<TNode, TEdge, TVal> CalculateShortestPaths<TVal>(SparseGraph<TNode, TEdge> graph,
            PathLogic<TEdge, TVal> pathLogic, TNode start)
        {
            return CalculateShortestPaths(graph, pathLogic, start.Id);
        }

        /// <summary>
        /// Finds negative cycle
        /// </summary>
        /// <param name="searchResult">Search result returned by <code>CalculateShortestPaths</code> method</param>
        /// <returns></returns>
        public static Path<TNode, TEdge, TVal> GetNegativeCycle<TVal>(SparseGraph<TNode, TEdge> graph,
            PathLogic<TEdge, TVal> pathLogic, PathSearchResult<TNode, TEdge, TVal> searchResult)
        {
            var lastNodeId = -1;
            foreach (var edge in graph.Edges)
            {
                if (pathLogic.E(searchResult.Distance[edge.From.Id], pathLogic.UnreachableValue))
                    continue;

                var newDist = pathLogic.Relax(searchResult.Distance[edge.From.Id], edge);
                if (pathLogic.Gt(searchResult.Distance[edge.To.Id], newDist))
                {
                    searchResult.Distance[edge.To.Id] = newDist;
                    searchResult.Ancestors[edge.To.Id] = edge;
                    lastNodeId = edge.To.Id;
                }
            }

            if (lastNodeId == -1)
                return null;

            var visited = new bool[graph.NodesCnt];
            for (; !visited[lastNodeId]; lastNodeId = searchResult.Ancestors[lastNodeId].From.Id)
            {
                visited[lastNodeId] = true;
            }
            var pathEdges = new List<TEdge> { searchResult.Ancestors[lastNodeId] };
            for (var nodeId = searchResult.Ancestors[lastNodeId].From.Id; nodeId != lastNodeId; nodeId = searchResult.Ancestors[nodeId].From.Id)
            {
                pathEdges.Add(searchResult.Ancestors[nodeId]);
            }
            pathEdges.Reverse();

            return new Path<TNode, TEdge, TVal>(graph, pathLogic.ZeroValue, pathEdges);
        }


        private static bool ExecutePhase<TVal>(SparseGraph<TNode, TEdge> graph, PathLogic<TEdge, TVal> pathLogic,
            PathSearchResult<TNode, TEdge, TVal> res)
        {
            var anyAction = false;
            foreach (var edge in graph.Edges)
            {
                if (pathLogic.E(res.Distance[edge.From.Id], pathLogic.UnreachableValue))
                    continue;

                var newDist = pathLogic.Relax(res.Distance[edge.From.Id], edge);
                if (pathLogic.Gt(res.Distance[edge.To.Id], newDist) && !res.IsAncestor(edge.From.Id, edge.To.Id))
                {
                    res.Distance[edge.To.Id] = newDist;
                    res.Ancestors[edge.To.Id] = edge;
                    anyAction = true;
                }
            }
            return anyAction;
        }
    }
}
