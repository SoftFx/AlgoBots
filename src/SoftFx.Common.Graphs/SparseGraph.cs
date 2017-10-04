using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SoftFx.Common.Graphs
{
    /// <summary>
    /// Represents basic dynamic directed sparse graph suitable for most algorithms
    /// </summary>
    public class SparseGraph<TNode, TEdge> where TNode : Node where TEdge : Edge<TNode>
    {
        private readonly List<TNode> _nodes;
        private readonly List<List<TEdge>> _nodeInEdges;
        private readonly List<List<TEdge>> _nodeOutEdges;
        private readonly List<TEdge> _edges;


        /// <summary>
        /// Graph display name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Graph nodes count
        /// </summary>
        public int NodesCnt => _nodes.Count;

        /// <summary>
        /// Graph edges count
        /// </summary>
        public int EdgesCnt => _edges.Count;

        /// <summary>
        /// All availiable graph nodes
        /// </summary>
        public IReadOnlyList<TNode> Nodes => _nodes;

        /// <summary>
        /// All avaliable graph edges
        /// </summary>
        public IReadOnlyList<TEdge> Edges => _edges;

        /// <summary>
        /// Gets node by id
        /// </summary>
        /// <returns>Throws exception if there is no node with specified Id, otherwise will return requested node</returns>
        public TNode this[int id]
        {
            get
            {
                CheckNodeId(id);
                return _nodes[id];
            }
        }

        /// <summary>
        /// Gets edge by it From and To nodes ids
        /// </summary>
        /// <returns>Throws exception if there is no edge with specified Ids or there are multiple edges, otherwise will return requested edge</returns>
        public TEdge this[int fromId, int toId]
        {
            get
            {
                CheckNodeId(fromId);
                CheckNodeId(toId);
                if (_nodeOutEdges[fromId].Count(e => e.To.Id == toId) > 1)
                    throw new GraphException($"There are multiple edges between nodes {fromId} and {toId}");
                return _nodeOutEdges[fromId].FirstOrDefault(e => e.To.Id == toId);
            }
        }


        public SparseGraph()
        {
            _nodes = new List<TNode>();
            _nodeInEdges = new List<List<TEdge>>();
            _nodeOutEdges = new List<List<TEdge>>();
            _edges = new List<TEdge>();
        }


        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Name);
            sb.AppendLine($"Nodes(cnt = {NodesCnt}): {string.Join(", ", Nodes.Select(n => n.ToString()))}");
            sb.AppendLine($"Edges(cnt = {EdgesCnt}):");
            foreach (var edge in Edges)
            {
                sb.AppendLine(edge.ToString());
            }
            return sb.ToString();
        }


        /// <summary>
        /// Gets incoming edges for node with specified id
        /// </summary>
        /// <returns>Throws exception if there is no node with specified Id, otherwise will return requested edge</returns>
        public IReadOnlyList<TEdge> GetNodeInEdges(int id)
        {
            CheckNodeId(id);
            return _nodeInEdges[id];
        }

        /// <summary>
        /// Gets outgoing edges for node with specified id
        /// </summary>
        /// <returns>Throws exception if there is no node with specified Id, otherwise will return requested edges</returns>
        public IReadOnlyList<TEdge> GetNodeOutEdges(int id)
        {
            CheckNodeId(id);
            return _nodeOutEdges[id];
        }

        /// <summary>
        /// Adds node to graph and assigns new id to it
        /// </summary>
        public virtual void AddNode(TNode node)
        {
            node.Id = NodesCnt;
            _nodes.Add(node);
            _nodeInEdges.Add(new List<TEdge>());
            _nodeOutEdges.Add(new List<TEdge>()); ;
        }

        /// <summary>
        /// Adds directed edge to graph
        /// </summary>
        public virtual void AddEdge(TEdge edge)
        {
            if (edge == null)
                throw new GraphException($"Can't add invalid edge");

            _edges.Add(edge);
            _nodeInEdges[edge.To.Id].Add(edge);
            _nodeOutEdges[edge.From.Id].Add(edge);
        }

        /// <summary>
        /// Removes directed edge from graph
        /// </summary>
        public virtual void RemoveEdge(TEdge edge)
        {
            if (edge == null)
                throw new GraphException($"Can't remove invalid edge");

            _edges.Remove(edge);
            _nodeInEdges[edge.To.Id].Remove(edge);
            _nodeOutEdges[edge.From.Id].Remove(edge);
        }

        /// <summary>
        /// Removes directed edge from graph
        /// </summary>
        public void RemoveEdge(int fromId, int toId)
        {
            var edge = this[fromId, toId];
            if (edge == null)
                throw new GraphException($"Edge {fromId} - {toId} doesn't exists");
            RemoveEdge(edge);
        }

        /// <summary>
        /// Remove all edges for node with specified id from graph
        /// </summary>
        public void IsolateNode(int id)
        {
            CheckNodeId(id);
            var edges = _nodeInEdges[id].Concat(_nodeOutEdges[id]).ToArray();
            foreach (var edge in edges)
            {
                RemoveEdge(edge);
            }
        }

        /// <summary>
        /// Isolates all excess nodes and removes them
        /// </summary>
        public void ShrinkGraph(int nodesCnt)
        {
            while (NodesCnt > nodesCnt)
            {
                IsolateNode(NodesCnt - 1);
                _nodeInEdges.RemoveAt(NodesCnt - 1);
                _nodeOutEdges.RemoveAt(NodesCnt - 1);
                _nodes.RemoveAt(NodesCnt - 1);
            }
        }

        /// <summary>
        /// Creates matrix representation of current graph state, using edge conversion func.
        /// </summary>
        /// <param name="converter">Conversion func for edge. Can be called multiple times in case of multigraph, current snapshot value is provided as second param</param>
        /// <param name="defaultValue">Value which will be set for the whole matrix, before applying edges</param>
        /// <returns>Matrix graph snapshot</returns>
        public T[,] GetMatrixSnapshot<T>(Func<TEdge, T, T> converter, T defaultValue = default(T))
        {
            var snapshot = new T[NodesCnt, NodesCnt];
            for (var i = 0; i < NodesCnt; i++)
                for (var j = 0; j < NodesCnt; j++)
                {
                    snapshot[i, j] = defaultValue;
                }

            foreach (var edge in _edges)
            {
                snapshot[edge.From.Id, edge.To.Id] = converter(edge, snapshot[edge.From.Id, edge.To.Id]);
            }

            return snapshot;
        }

        /// <summary>
        /// Creates a graph snapshot with old instances of nodes and new instances of edges
        /// </summary>
        /// <returns>Graph snapshot</returns>
        public SparseGraph<TNode, TNewEdge> GetSnapshot<TNewEdge>(Func<TEdge, TNewEdge> converter) where TNewEdge : Edge<TNode>
        {
            var snapshot = new SparseGraph<TNode, TNewEdge>();

            snapshot._nodes.AddRange(_nodes);
            for (var i = 0; i < NodesCnt; i++)
            {
                snapshot._nodeInEdges.Add(new List<TNewEdge>());
                snapshot._nodeOutEdges.Add(new List<TNewEdge>());
            }
            foreach (var edge in Edges)
            {
                var newEdge = converter(edge);
                if (newEdge != null)
                {
                    snapshot.AddEdge(newEdge);
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Creates a graph snapshot with old instances of nodes and new instances of edges
        /// </summary>
        /// <returns>Graph snapshot</returns>
        public TGraph GetSnapshot<TGraph, TNewEdge>(Func<TEdge, TNewEdge> converter)
            where TGraph : SparseGraph<TNode, TNewEdge>, new() where TNewEdge : Edge<TNode>
        {
            var snapshot = new TGraph();

            snapshot._nodes.AddRange(_nodes);
            for (var i = 0; i < NodesCnt; i++)
            {
                snapshot._nodeInEdges.Add(new List<TNewEdge>());
                snapshot._nodeOutEdges.Add(new List<TNewEdge>());
            }
            foreach (var edge in Edges)
            {
                var newEdge = converter(edge);
                if (newEdge != null)
                {
                    snapshot.AddEdge(newEdge);
                }
            }

            return snapshot;
        }


        protected bool IsNodeExists(int id)
        {
            return id >= NodesCnt;
        }

        protected void CheckNodeId(int id)
        {
            if (IsNodeExists(id))
                throw new GraphException($"Node Id = {id}. Expected in range [0; {NodesCnt - 1}]");
        }
    }


    public class SparseGraph : SparseGraph<Node, Edge<Node>>
    {
        public double[,] GetMatrixSnapshot(double defaultValue = 0.0)
        {
            return GetMatrixSnapshot((edge, val) => Math.Min(edge.Weight, val), defaultValue);
        }
    }
}
