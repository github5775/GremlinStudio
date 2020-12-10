using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Graphyte
{
    /// <summary>
    /// Custom collection implementation for edges. Technically an <see
    /// cref="http://en.wikipedia.org/wiki/Incidence_matrix">incidence matrix</see>
    /// structure.
    /// </summary>
    /// <remarks>
    /// If you take the adjmatrix[n] you get the collection of parent of vertex n
    /// </remarks>
    internal sealed class EdgeCollection : IEnumerable<Edge>
    {
        #region Fields
        private Graph graph;
        private readonly Dictionary<Vertex, List<Vertex>> adjmatrix;
        #endregion

        #region Constructor
        internal EdgeCollection(Graph parentGraph)
        {
            graph = parentGraph;
            adjmatrix = new Dictionary<Vertex, List<Vertex>>();
        }

        #endregion

        #region Methods
        /// <summary>
        /// Adds an edge to the collection.
        /// </summary>
        /// <param name="from">The From or parent vertex.</param>
        /// <param name="to">The To or child vertex.</param>
        public bool AddEdge(Vertex from, Vertex to)
        {
            ////////orig
            //////if (!adjmatrix.ContainsKey(to))
            //////    adjmatrix.Add(to, new List<Vertex>());
            //////if (!adjmatrix[to].Contains(from))
            //////    adjmatrix[to].Add(from);

            Debug.WriteLine($"RQ for FROM {from.ID} TO: {to.ID}");

            if (!adjmatrix.ContainsKey(to))
            {
                adjmatrix.Add(to, new List<Vertex>());
            }
            if (!adjmatrix.ContainsKey(from))
            {
                adjmatrix.Add(from, new List<Vertex>());
            }
            if (!adjmatrix[to].Contains(from) && !adjmatrix[from].Contains(to))
            {
                adjmatrix[to].Add(from);
                adjmatrix[from].Add(to);
                return true;
            }
            else
            {
                Debug.WriteLine($"{from.ID} already contains edge to: {to.ID}");
                return false;
            }
        }
        /// <summary>
        /// Removes an edge from the collection.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public void RemoveEdge(Vertex from, Vertex to)
        {
            if (!adjmatrix.ContainsKey(from))
                return;
            if (!adjmatrix[from].Contains(to))
                return;
            adjmatrix[from].Remove(to);
        }
        /// <summary>
        /// Removes the vertex from the matrix.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        public void RemoveVertex(Vertex vertex)
        {
            if (adjmatrix.ContainsKey(vertex))
                adjmatrix.Remove(vertex); //this removes all the parent bindings
            foreach (Vertex key in adjmatrix.Keys)
            {
                if (adjmatrix[key].Contains(vertex))
                    adjmatrix[key].Remove(vertex);//this removes a children bindings
            }
        }

        /// <summary>
        /// Adds an edge.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="exists">if set to <c>true</c> [exists].</param>
        private void AddEdge(Vertex from, Vertex to, bool exists)
        {
            if (exists)
                AddEdge(from, to);
            else
                RemoveEdge(from, to);
        }

        /// <summary>
        /// Alternative access to the <see cref="AddEdge"/> and <see cref="RemoveEdge"/> methods.
        /// </summary>
        /// <value></value>
        public bool this[Vertex from, Vertex to]
        {
            set { AddEdge(from, to, value); }
            get
            {
                if (adjmatrix.ContainsKey(from) && adjmatrix[from].Contains(to))
                    return true;
                return false;
            }
        }
        /// <summary>
        /// Gets childrens of the specified vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns></returns>
        public List<Vertex> ChildrenOf(Vertex vertex)
        {
            var vertices = new List<Vertex>();
            foreach (Vertex key in adjmatrix.Keys)
            {
                if (adjmatrix[key].Contains(vertex))
                    vertices.Add(key);
            }
            return vertices;
        }
        /// <summary>
        /// Gets the parents of the specified vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns></returns>
        public List<Vertex> ParentsOf(Vertex vertex)
        {
            if (adjmatrix.ContainsKey(vertex))
            {
                return adjmatrix[vertex];
            }
            return new List<Vertex>();
        }


        /// <summary>
        /// Returns whether there is an edge between the specified vertices, independently of the direction.
        /// </summary>
        /// <param name="a">From.</param>
        /// <param name="b">To.</param>
        /// <returns>
        /// 	<c>true</c> if the specified a has edge; otherwise, <c>false</c>.
        /// </returns>
        internal bool HasEdge(Vertex a, Vertex b)
        {
            if (adjmatrix.ContainsKey(a))
            {
                if (adjmatrix[a].Contains(b))
                {
                    return true;
                }

            }
            if (adjmatrix.ContainsKey(b))
            {
                if (adjmatrix[b].Contains(a))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        internal void Clear()
        {
            adjmatrix.Clear();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<Edge> GetEnumerator()
        {
            foreach (Vertex key in adjmatrix.Keys)
            {
                foreach (Vertex vertex in adjmatrix[key])
                {
                    yield return new Edge { From = key,To = vertex};
                }
            }
        }


        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}