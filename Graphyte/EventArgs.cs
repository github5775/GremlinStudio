using System;

namespace Graphyte
{
    /// <summary>
    /// Event argument to pass on vertex information.
    /// </summary>
    public sealed class VertexEventArgs : EventArgs
    {
        public bool Handled;
        public Vertex Vertex { get; internal set; }
        public VertexEventArgs(Vertex vertex)
        {
            Vertex = vertex;
        }
    }
    /// <summary>
    /// Event argumen to pass on edge information.
    /// </summary>
    public sealed class EdgeEventArgs : EventArgs
    {
        public Vertex From { get; set; }
        public Vertex To { get; set; }
        public string Label { get; set; }
        public EdgeEventArgs(Vertex from, Vertex to, string label) : this(from,to)
        {
            Label = label;
        }
        public EdgeEventArgs(Vertex from, Vertex to)
        {
            From = from;
            To = to;
            Label = "";
        }
        public EdgeEventArgs(Edge edge)
        {
            From = edge.From;
            To = edge.To;
            Label = "";
        }
    }
    
}