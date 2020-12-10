using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Graphyte
{
    /// <summary>
    /// Custom collection for vertices.
    /// </summary>
    internal sealed class VertexCollection : Collection<Vertex>
    {
        private readonly Graph graph;

        internal VertexCollection(Graph parentGraph)
        {
            graph = parentGraph;
        }

        /// <summary>
        /// Gets the children vertices of the specified vertex.
        /// </summary>
        /// <param name="ofVertex">A vertex in the collection.</param>
        /// <returns></returns>
        public IList<Vertex> Children(Vertex ofVertex)
        {
            return graph.AdjacentVertices(ofVertex);
        }

        /// <summary>
        /// Gets the parent vertices of the specified vertex.
        /// </summary>
        /// <param name="ofVertex">A vertex in the collection.</param>
        /// <returns></returns>
        public IList<Vertex> Parents(Vertex ofVertex)
        {
            return graph.AdjacentVertices(ofVertex);
        }

        
       
    }
}