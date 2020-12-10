using System.Collections.Generic;
using System.Windows;

namespace Graphyte
{
/// <summary>
/// Helper class to store visual and physical description for the vertices.
/// </summary>
public sealed class VertexState
{
    public Vertex Vertex;
    public Point Position;
    public Point Velocity;
    public VisualVertex Visual;
    public List<KeyValuePair<Vertex, VisualEdge>> ChildrenLines = new List<KeyValuePair<Vertex, VisualEdge>>();
    public List<KeyValuePair<Vertex, VisualEdge>> ParentLines = new List<KeyValuePair<Vertex, VisualEdge>>();
}

   
  
}