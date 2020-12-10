using System;
using System.Windows;

namespace Graphyte
{
    /// <summary>
    /// A graph vertex.
    /// </summary>
    public sealed class Vertex
    {
        internal Graph Graph { get; set; }
        /// <summary>
        /// Gets the empty vertex.
        /// </summary>
        /// <value>The empty.</value>
        public static Vertex Empty
        {
            get { return new Vertex { Title = string.Empty, Info = string.Empty, Type = VertexType.NotSpecified }; }
        }
        #region Constructor
        ///<summary>
        ///Default constructor
        ///</summary>
        public Vertex()
        {
            ID = Guid.NewGuid().ToString();
        }
        #endregion

        #region Properties

        /// <summary>
        /// If <c>true</c> this vertex will not be moved by the layout, it will however have an effect on other vertices if linked to it.
        /// </summary>
        public bool IsFixed;

        /// <summary>
        /// Sets the initial position of the vertex. If used with the <see cref="IsFixed"/> property this will hold the vertex in place at the specified location while still participating 
        /// in the layout process (i.e. have an effect on the linked vertices).
        /// </summary>
        public Point InitialPosition;
        /// <summary>
        /// An identifier, by default this is set to a Guid.
        /// </summary>
        public string ID;

        /// <summary>
        /// The vertex type
        /// </summary>
        public VertexType Type;
        /// <summary>
        /// The text appearing in the diagram.
        /// </summary>
        public string Title;
        /// <summary>
        /// The info you want to attach to the vertex.
        /// </summary>
        public object Info; 
        #endregion

    }
}