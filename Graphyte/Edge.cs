using System.Windows.Shapes;

namespace Graphyte
{
    /// <summary>
    /// Encapsulating class for an edge.
    /// </summary>
    /// <remarks>
    /// An edge is being converted to a <see cref="Line">Line </see>on the presentation
    /// level.
    /// </remarks>
    public class Edge
    {
        public Vertex From;
        public Vertex To;

        #region Construtor

        public Edge()
        {
            
        }
        public Edge(Vertex from, Vertex to)
        {
            From = from;
            To = to;
        }

        #endregion
    }
}