using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
namespace Graphyte
{

    /// <summary>
    /// The model or graph.
    /// </summary>
    internal sealed class Graph
    {

        #region Events

        public event EventHandler<VertexEventArgs> VertexAdded;
        public event EventHandler<VertexEventArgs> VertexRemoved;
        public event EventHandler<EdgeEventArgs> EdgeAdded;
        public event EventHandler<EdgeEventArgs> EdgeRemoved;
        #endregion

        #region Fields

        private readonly EdgeCollection edges;
        private readonly VertexCollection vertices;
        #endregion

        #region Properties
        /// <summary>
        /// Gets a random vertex from the graph.
        /// </summary>
        /// <value>The random vertex.</value>
        public Vertex RandomVertex
        {
            get
            {
                var rnd = new Random();
                return vertices[rnd.Next(0, vertices.Count)];
            }
        }
        /// <summary>
        /// Gets the vertices in this graph.
        /// </summary>
        /// <value>The vertices.</value>
        internal VertexCollection Vertices
        {
            get { return vertices; }
        }
        internal EdgeCollection Edges
        {
            get { return edges; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Graph"/> class.
        /// </summary>
        public Graph()
        {
            edges = new EdgeCollection(this);
            vertices = new VertexCollection(this);
        }
        #endregion

        #region Methods
        public void Clear()
        {
            edges.Clear();
            vertices.Clear();
        }

        public bool HasEdge(Vertex from, Vertex to)
        {
            return edges.HasEdge(from, to);
        }

        public IList<Vertex> AdjacentVertices(Vertex a)
        {
            return ChildrenOf(a).Union(ParentsOf(a)).ToList();
        }

        public void AddEdge(Vertex from, Vertex to)
        {
            AddEdge(from, to, "");
        }

        public void AddEdge(Vertex from, Vertex to, string label)
        {
            if (!vertices.Contains(from))
            {
                Debug.WriteLine($"-------> ADDEDGE: DOES NOT EXIST: {from.ID}: {from.Title}");
            }
            else if (!vertices.Contains(to))
            {
                Debug.WriteLine($"-------> ADDEDGE: DOES NOT EXIST: {to.ID}: {to.Title}");
            }
            //if (!vertices.Contains(from) || !vertices.Contains(to))
            //    throw new Exception("One or both of the vertices attached to the edge is not contained in the graph.");
            if (edges.AddEdge(from, to))
                RaiseEdgeAdded(from, to, label);

        }
        public void RemoveEdge(Vertex from, Vertex to)
        {
            edges.RemoveEdge(from, to);
            RaiseEdgeRemoved(from, to);
        }

        public void AddVertex(Vertex vertex)
        {
            //HACK: demo restriction
#if DEMO
            if(vertices.Count()>=10)
                throw new ApplicationException("The demo doesn't allow you to add more than ten vertices to the diagram. Please acquire the commercial version for full access.");

#endif
            if (!vertices.Contains(vertex))
            {
                vertices.Add(vertex);
                vertex.Graph = this; //keep an internal binding with the graph alltogether
                RaiseVertexAdded(vertex);
            }
        }

        public void RemoveVertex(Vertex vertex)
        {
            if (vertices.Contains(vertex))
            {
                vertices.Remove(vertex);
                edges.RemoveVertex(vertex);
                RaiseVertexRemoved(vertex);
            }
        }

        public List<Vertex> ChildrenOf(Vertex vertex)
        {
            return edges.ChildrenOf(vertex);
        }
        public List<Vertex> ParentsOf(Vertex vertex)
        {
            return edges.ParentsOf(vertex);
        }

        #region Raisers
        private void RaiseVertexAdded(Vertex vertex)
        {
            EventHandler<VertexEventArgs> handler = VertexAdded;
            if (handler != null)
            {
                handler(this, new VertexEventArgs(vertex));
            }
        }

        private void RaiseVertexRemoved(Vertex vertex)
        {
            EventHandler<VertexEventArgs> handler = VertexRemoved;
            if (handler != null)
            {
                handler(this, new VertexEventArgs(vertex));
            }
        }
        private void RaiseEdgeAdded(Vertex from, Vertex to, string label)
        {
            EventHandler<EdgeEventArgs> handler = EdgeAdded;
            if (handler != null)
            {
                handler(this, new EdgeEventArgs(from, to, label));
            }
        }
        private void RaiseEdgeAdded(Vertex from, Vertex to)
        {
            EventHandler<EdgeEventArgs> handler = EdgeAdded;
            if (handler != null)
            {
                handler(this, new EdgeEventArgs(from, to));
            }
        }
        private void RaiseEdgeRemoved(Vertex from, Vertex to)
        {
            EventHandler<EdgeEventArgs> handler = EdgeRemoved;
            if (handler != null)
            {
                handler(this, new EdgeEventArgs(from, to));
            }
        }
        #endregion
        #endregion
    }


}
