using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace Graphyte
{
    /// <summary>
    /// Utility class.
    /// </summary>
    public static class Utils
    {
        private static readonly Random Rnd = new Random();
        private static readonly Stream SampleNamesStream = typeof(Utils).Assembly.GetManifestResourceStream("Graphyte.physics.txt");
        private static readonly string[] Names = ReadAllLines(SampleNamesStream);
        public static VertexContent RandomTitle()
        {
            var fetch = Names[Rnd.Next(0, Names.Length)];
            var title = string.Empty;
            var info = string.Empty;
            if (fetch.Contains("/"))
            {
                title = fetch.Substring(0, fetch.IndexOf("/"));
                info = fetch.Substring(fetch.IndexOf("/") + 1);
            }
            else
            {
                title = fetch;
            }

            return new VertexContent { Info = info,Title = title};

        }
        public static string[] ReadAllLines(Stream s)
        {
            var reader = new StreamReader(s);
            var allLines = new List<string>();

            while (!reader.EndOfStream)
            {
                allLines.Add(reader.ReadLine().Trim());
            }
            return allLines.ToArray();
        }

        /// <summary>
        /// Returns the children vertices, i.e. the the target vertices of an arrow/edge.
        /// </summary>
        /// <param name="from">From.</param>
        /// <returns></returns>
        public static List<Vertex> ChildrenVertices(this Vertex from)
        {
            return from.Graph.ChildrenOf(from);
        }
        /// <summary>
        /// Returns the parent vertices, i.e. the the source vertices of an arrow/edge
        /// </summary>
        /// <param name="to">To.</param>
        /// <returns></returns>
        public static List<Vertex> ParentVertices(this Vertex to)
        {
            return to.Graph.ParentsOf(to);
        }
        /// <summary>
        /// Returns true if there is either a from/to or a to/from edge between the current vertex and the given vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="otherVertex">The other vertex.</param>
       
        public static bool HasEdgeWith(this Vertex vertex, Vertex otherVertex)
        {
            return vertex.Graph.HasEdge(vertex, otherVertex);
        }
    }
    public struct VertexContent
    {
        public string Info;
        public string Title;
    }
}