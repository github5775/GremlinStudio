using System.Windows;

namespace Graphyte
{
    /// <summary>
    /// Keeps track of what is begin dragged.
    /// </summary>
    internal struct VertexDragState
    {
        public bool IsDragging;
        public VertexState VertexBeingDragged;
        public Point OffsetWithinVertex;
    };
}