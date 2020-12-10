
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;

namespace Graphyte
{

    /// <summary>
    /// This is the diagramming control you can embed in your WPF application.
    /// </summary>
    public class GraphCanvas : Canvas
    {
        #region Events

        /// <summary>
        /// Occurs when the mouse is hovering a vertex.
        /// </summary>
        public event EventHandler<VertexEventArgs> VertexHovering;
        /// <summary>
        /// Occurs when a vertex is clicked.
        /// </summary>
        public event EventHandler<VertexEventArgs> VertexClick;
        /// <summary>
        /// Occurs when a vertex is clicked.
        /// </summary>
        public event EventHandler<VertexEventArgs> VertexDoubleClick;
        /// <summary>
        /// Occurs when a vertex has been deleted.
        /// </summary>
        public event EventHandler<VertexEventArgs> VertexDelete;
        /// <summary>
        /// Occurs when a vertex was added.
        /// </summary>
        public event EventHandler<VertexEventArgs> VertexAdd;
        /// <summary>
        /// Occurs when the first vertex of a pair was slected.
        /// </summary>
        public event EventHandler<VertexEventArgs> VertexSelect1;
        /// <summary>
        /// Occurs when the second vertex of a pair was selected.
        /// </summary>
        public event EventHandler<VertexEventArgs> VertexSelect2;
        /// <summary>
        /// Occurs when an edge was added.
        /// </summary>
        public event EventHandler<EdgeEventArgs> EdgeAdd;
        /// <summary>
        /// Occurs when an edgde was deleted.
        /// </summary>
        public event EventHandler<EdgeEventArgs> EdgeDelete;
        /// <summary>
        /// Occurs when the mouse is hovering a edge.
        /// </summary>
        public event EventHandler<EdgeEventArgs> EdgeHovering;
        /// <summary>
        /// Occurs when a edge is clicked.
        /// </summary>
        public event EventHandler<EdgeEventArgs> EdgeClick;

        #endregion

        #region Constants

        private const double vertexHeight = 27D;
        private const double lineThickness = 1D;
        private const double attractionStrength = 0.9D;
        private const double repulsionStrength = 1200D;
        private const double centroidSpeed = 2D;
        private const double timeStep = 0.95;
        private const double damping = 0.90;
        private const double lineHighlightThickness = 2D;
        private const double repulsionClipping = 200D;

        #endregion

        #region Fields
        private DispatcherTimer timer;
        private int bubbleAmount = 15;
        private VertexDragState dragState;
        private readonly Random rnd = new Random();
        private readonly DispatcherTimer layoutTimer;
        private readonly Graph graph;
        private readonly Dictionary<Vertex, VertexState> vertexStates;
        private readonly Brush vertexBackground;
        private readonly Brush vertexDefaultBorder;
        private readonly Brush vertexHighlightBorder;
        private readonly Brush vertexSelectedBorder;
        private Brush _lineBrush;
        public Brush LineBrush { get { return _lineBrush; } set { _lineBrush = value; } }
        private double currentZoom = 1D;
        #endregion

        #region Properties

        #region IsHomeDeletionEnabled

        /// <summary>
        /// IsHomeDeletionEnabled Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsHomeDeletionEnabledProperty =
            DependencyProperty.Register("IsHomeDeletionEnabled", typeof(bool), typeof(GraphCanvas),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets the IsHomeDeletionEnabled property.  This dependency property 
        /// indicates whether the canvas home vertex can be deleted. 
        /// </summary>
        public bool IsHomeDeletionEnabled
        {
            get { return (bool)GetValue(IsHomeDeletionEnabledProperty); }
            set { SetValue(IsHomeDeletionEnabledProperty, value); }
        }

        #endregion

        public bool UseCentralForce { get; set; }

        /// <summary>
        /// Gets the first selected vertex of the vertex pair which allows you to add a new edge through the <see cref="AddEdgeToSelected"/> method.
        /// </summary>
        /// <value>The selected vertex.</value>
        public Vertex Selected1 { get; internal set; }
        /// <summary>
        /// Gets the second selected vertex of the vertex pair which allows you to add a new edge through the <see cref="AddEdgeToSelected"/> method.
        /// </summary>
        /// <value>The selected vertex.</value>
        public Vertex Selected2 { get; internal set; }

        internal bool ReadOnly { get; set; }
        /// <summary>
        /// Gets or sets the home vertex.
        /// </summary>
        /// <value>The home.</value>
        public Vertex Home { get; set; }
        internal bool BlockEvent { get; set; }
        /// <summary>
        /// Gets the graph or model on which the diagram is based.
        /// </summary>
        /// <value>The graph.</value>
        internal Graph Graph
        {
            get { return graph; }

        }
        /// <summary>
        /// Gets the vertices from the graph.
        /// </summary>
        /// <value>A read-only collection of the graph vertices. To add or delete vertices use the appropriate methods instead of this collection.</value>
        public ReadOnlyCollection<Vertex> Vertices
        {
            get { return new ReadOnlyCollection<Vertex>(graph.Vertices.ToList()); }
        }
        /// <summary>
        /// Returns a random vertex from the graph.
        /// </summary>
        /// <value>The random vertex.</value>
        public Vertex RandomVertex
        {
            get
            {
                return graph.RandomVertex;

            }
        }

        /// <summary>
        /// Gets the center of gravity of the diagram.
        /// </summary>
        /// <value>The center of gravity.</value>
        public Point CenterOfGravity
        {
            get
            {
                if (VertexStates.Count == 0) return new Point(0, 0);
                var centroid = new Point();

                foreach (var state in VertexStates.Values)
                {
                    centroid.X += state.Position.X;
                    centroid.Y += state.Position.Y;
                }
                centroid.X = centroid.X / VertexStates.Count;
                centroid.Y = centroid.Y / VertexStates.Count;
                return centroid;
            }
        }

        protected Dictionary<Vertex, VertexState> VertexStates
        {
            get { return vertexStates; }
        }

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphCanvas"/> class.
        /// </summary>
        public GraphCanvas()
        {
            Loaded += GraphCanvas_Loaded;
            graph = new Graph();
            graph.VertexAdded += graph_VertexAdded;
            graph.EdgeAdded += graph_EdgeAdded;
            graph.EdgeRemoved += graph_EdgeRemoved;
            graph.VertexRemoved += graph_VertexRemoved;
            UseCentralForce = true;

            vertexStates = new Dictionary<Vertex, VertexState>();
            layoutTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 10) };
            layoutTimer.Tick += TimerTick;

            //if (Application.Current != null)
            //{
            vertexBackground = Application.Current.Resources["VertexBackground"] as LinearGradientBrush;
            LineBrush = Application.Current.Resources["EdgeStroke"] as LinearGradientBrush;
            vertexDefaultBorder = Application.Current.Resources["VertexDefaultBorder"] as SolidColorBrush;
            vertexHighlightBorder = Application.Current.Resources["VertexHighlightBorder"] as SolidColorBrush;
            vertexSelectedBorder = Application.Current.Resources["VertexSelectedBorder"] as SolidColorBrush;

            //}
            //else
            {
                MouseMove += Canvas_MouseMove;
                MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
                MouseLeave += Canvas_MouseLeave;
                MouseLeftButtonDown += GraphCanvas_MouseLeftButtonDown;
                //vertexBackground = Brushes.White;
                //vertexSelectedBorder = vertexHighlightBorder = vertexDefaultBorder = LineBrush = Brushes.Black;
            }
        }
        /// <summary>
        /// Handles the MouseLeftButtonDown event of the GraphCanvas control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void GraphCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                if (Selected1 != null)
                {
                    VertexStates[Selected1].Visual.BorderBrush = vertexDefaultBorder;
                    Selected1 = null;
                }
                if (Selected2 != null)
                {
                    VertexStates[Selected2].Visual.BorderBrush = vertexDefaultBorder;
                    Selected2 = null;
                }
            }
        }

        /// <summary>
        /// Determines whether there is an edge between the given vertices
        /// </summary>
        /// <param name="from"> The start vertex.</param>
        /// <param name="to">The end vertex.</param>
        /// <returns>
        /// 	<c>true</c> if the specified vertices have an edge in the current graph; otherwise, <c>false</c>.
        /// </returns>
        public bool HasEdge(Vertex from, Vertex to)
        {
            return graph.HasEdge(from, to);
        }
        /// <summary>
        /// Determines whether the specified vertex is part of the current graph.
        /// </summary>
        /// <param name="vertex">Some vertex.</param>
        /// <returns>
        /// 	<c>true</c> if the specified vertex is part of the graph; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsVertex(Vertex vertex)
        {
            return graph.Vertices.Contains(vertex);
        }

        /// <summary>
        /// Handles the Loaded event of the GraphCanvas control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void GraphCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            StartLayout();
        }

        /// <summary>
        /// Adds the home vertex.
        /// </summary>
        private void AddHomeVertex()
        {
            var home = new Vertex { Type = VertexType.Standard, Title = "Home" };
            AddVertex(home);
            Home = home;
        }

        private void BubbleVertices()
        {
            timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 500) };
            timer.Tick += timer_Tick;

            timer.Start();
        }

        public delegate void testje(Vertex source);
        void timer_Tick(object sender, EventArgs e)
        {
            if (Graph.Vertices.Count < bubbleAmount)
                Dispatcher.Invoke(new testje(AttachRandomVertexTo), new object[] { null });
            //Dispatcher.BeginInvoke(DispatcherPriority.Render,
            //                       Delegate.CreateDelegate(typeof(testje),
            //                                               this,this.GetType().GetMethod("AttachRandomVertexTo")),(Vertex)null);
            else
                timer.Stop();
        }
        /// <summary>
        /// Adds some bubbling vertices to the diagram.
        /// </summary>
        /// <see cref="AddRandomVertices"/>
        /// <param name="amount">The amount of vertices to add.</param>
        public void AddBubblingVertices(int amount)
        {
            bubbleAmount = amount;
            BubbleVertices();
        }

        /// <summary>
        /// Adds some random vertices to the diagram.
        /// </summary>
        /// <seealso cref="AddBubblingVertices"/>
        /// <param name="amount">The amount of vertices to add.</param>
        public void AddRandomVertices(int amount)
        {
            ReadOnly = true;
            for (var i = 0; i < amount; i++)
            {
                AttachRandomVertexTo(null);
            }
            ReadOnly = false;
        }

        public void LoadSampleGraph()
        {
            StopLayout();
            NewDiagram(false);
            BuildGraph(GetType().Assembly.GetManifestResourceStream("Graphite.SampleGraph.txt"));
            //XDocument xdoc = XDocument.Load(GetType().Assembly.GetManifestResourceStream("Graphite.SampleGraph.xml"));
            //BuildGraph(xdoc);


            StartLayout();
        }

        /// <summary>
        /// Loads data from a network location.
        /// <para></para>
        /// <para>            </para>
        /// <para>           
        /// graphite.Load(&quot;http://www.orbifold.net/test.txt&quot;);</para>
        /// </summary>
        /// <remarks>
        /// Very, very important, read the <see
        /// cref="http://msdn.microsoft.com/en-us/library/cc645032(VS.95).aspx">security
        /// access restriction in Silverlight</see>. You need to add the
        /// <b>ClientAccessPolicy.xml</b> file in the root of the server in order to give
        /// Silverlight access to the file you want to read. Something like:
        /// <para>             </para>
        /// <para>         <c>    &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot;?&gt;</c></para>
        /// <code>              &lt;access-policy&gt;
        ///                 &lt;cross-domain-access&gt;
        ///                   &lt;policy &gt;
        ///                     &lt;allow-from&gt;
        ///                       &lt;domain uri=&quot;*&quot;/&gt;
        ///                     &lt;/allow-from&gt;
        ///                     &lt;grant-to&gt;
        ///                       &lt;resource path=&quot;/&quot; include-subpaths=&quot;false&quot;/&gt;
        ///                     &lt;/grant-to&gt;
        ///                  &lt;/policy&gt;
        ///                 &lt;/cross-domain-access&gt;
        ///               &lt;/access-policy&gt;</code>
        /// </remarks>
        /// <param name="networkpath"></param>
        public void LoadGraphFromFlatFile(string networkpath)
        {
            StopLayout();
            NewDiagram(false);
            var wc = new WebClient();
            wc.DownloadStringAsync(new Uri(networkpath));
            wc.DownloadStringCompleted += wc_DownloadStringCompleted;
        }

        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null) return;

            var encoding = new System.Text.UTF8Encoding();
            var ms = new MemoryStream(encoding.GetBytes(e.Result));
            BuildGraph(ms);
            StartLayout();

        }


        /// <summary>
        /// Clears the diagram.
        /// </summary>
        /// <param name="addHomeVertex">if set to <c>true</c> the home vertex will be added. </param>
        public void NewDiagram(bool addHomeVertex)
        {
            graph.Clear();
            VertexStates.Clear();
            Children.Clear();
            if (addHomeVertex)
                AddHomeVertex();
        }
        private void graph_EdgeRemoved(object sender, EdgeEventArgs e)
        {
            RemoveVisualEdge(e.From, e.To);
            RaiseEdgeEvent(new Edge(e.From, e.To), EdgeDelete);
        }

        /// <summary>
        /// Removes the visual edge corresponding to the given vertices.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        private void RemoveVisualEdge(Vertex from, Vertex to)
        {
            //note that the parentlines and childrenlines are keyed by the from-vertex

            //try to find the edge in the parent collection of the from-vertex
            var found = new KeyValuePair<Vertex, VisualEdge>();
            foreach (var line in VertexStates[from].ParentLines.Where(line => line.Key == to))
            {
                found = line;
                break;
            }
            if (found.Key != null)
            {
                VertexStates[from].ParentLines.Remove(found);
                VertexStates[to].ChildrenLines.Remove(found);
                Children.Remove(found.Value);
            }
            //try to find the edge in the parent collection of the to-vertex
            found = new KeyValuePair<Vertex, VisualEdge>();
            foreach (var line in VertexStates[to].ParentLines.Where(line => line.Key == from))
            {
                found = line;
                break;
            }
            if (found.Key != null)
            {
                VertexStates[to].ParentLines.Remove(found);
                VertexStates[from].ChildrenLines.Remove(found);
                Children.Remove(found.Value);
            }
        }

        void graph_VertexRemoved(object sender, VertexEventArgs e)
        {
            foreach (var line in VertexStates[e.Vertex].ParentLines)
            {
                Children.Remove(line.Value);
                var kvp = VertexStates[line.Key].ChildrenLines.SingleOrDefault(s => s.Key == e.Vertex);
                VertexStates[line.Key].ChildrenLines.Remove(kvp);
            }
            foreach (var line in VertexStates[e.Vertex].ChildrenLines)
            {
                Children.Remove(line.Value);
                var kvp = VertexStates[line.Key].ParentLines.SingleOrDefault(s => s.Key == e.Vertex);
                VertexStates[line.Key].ParentLines.Remove(kvp);
            }
            Children.Remove(VertexStates[e.Vertex].Visual);
            VertexStates.Remove(e.Vertex);
            RaiseVertexEvent(e.Vertex, VertexDelete);
        }



        /// <summary>
        /// Creates and attaches a random vertex to the given vertex.
        /// </summary>
        /// <param name="source">The vertex to which the new vertex will be attached. If <c>null</c> a random vertex from the diagram will be chosen.</param>
        public void AttachRandomVertexTo(Vertex source)
        {
            var content = Utils.RandomTitle();
            var n = new Vertex { Title = content.Title, Info = content.Info };
            AddVertex(n);
            if (graph.Vertices.Count > 0)
            {
                Vertex b;
                if (source == null)
                {
                    b = graph.Vertices[rnd.Next(0, graph.Vertices.Count)];
                    while (b == n)
                    {
                        b = graph.Vertices[rnd.Next(0, graph.Vertices.Count)];
                    }
                }
                else
                {
                    b = source;
                }
                AddEdge(n, b);
            }


        }

        void graph_EdgeAdded(object sender, EdgeEventArgs e)
        {
            if (BlockEvent) return;
            CreateVisualForEdge(e.From, e.To, e.Label);
            RaiseEdgeEvent(e.From, e.To, EdgeAdd);
        }

        void graph_VertexAdded(object sender, VertexEventArgs e)
        {
            if (BlockEvent) return;
            var ns = AddVertexState(e.Vertex);
            AddVisualVertex(new KeyValuePair<Vertex, VertexState>(e.Vertex, ns));



        }
        #endregion

        #region Methods
        public void SetVertexType(string id, VertexType vertexType)
        {
            foreach (var item in this.Children)
            {
                if (item is VisualVertex)
                {
                    if ((item as VisualVertex).Id == id)
                    {
                        (item as VisualVertex).Style = TryFindResource(vertexType.ToString()) as Style;
                    }
                }

            }
        }
        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseWheel"/> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseWheelEventArgs"/> that contains the event data.</param>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            //the 'zoom' 
            var source = e.Source as GraphCanvas;

            if (source == null) return;
            currentZoom += Math.Sign(e.Delta) * 0.2D;
            currentZoom = Math.Min(Math.Max(currentZoom, 0.2D), 2D);
            var st = new ScaleTransform(currentZoom, currentZoom, ActualWidth / 2, ActualHeight / 2);
            LayoutTransform = st;
        }

        #region Raisers
        /// <summary>
        /// Raises vertex events.
        /// </summary>
        /// <param name="vertex">The vertex information.</param>
        /// <param name="eventHandler">The event handler.</param>
        /// <returns></returns>
        private VertexEventArgs RaiseVertexEvent(Vertex vertex, EventHandler<VertexEventArgs> eventHandler)
        {
            var handler = eventHandler;
            if (handler != null)
            {
                var arg = new VertexEventArgs(vertex);
                handler(this, arg);
                return arg;
            }
            return null;
        }
        /// <summary>
        /// Raises edge events.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="eventHandler">The event handler.</param>
        /// <returns></returns>
        private void RaiseEdgeEvent(Edge edge, EventHandler<EdgeEventArgs> eventHandler)
        {
            var handler = eventHandler;
            if (handler != null)
            {
                var arg = new EdgeEventArgs(edge);
                handler(this, arg);
                return;
            }
            return;
        }
        private void RaiseEdgeEvent(Vertex from, Vertex to, EventHandler<EdgeEventArgs> eventHandler)
        {
            RaiseEdgeEvent(new Edge(from, to), eventHandler);
        }

        #endregion
        /// <summary>
        /// Code ran when the layout timer ticks
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void TimerTick(object sender, EventArgs e)
        {
            StepLayout();
        }
        /// <summary>
        /// Starts the layout.
        /// </summary>
        public void StartLayout()
        {
            if (!layoutTimer.IsEnabled)
                layoutTimer.Start();
        }
        /// <summary>
        /// Starts the layout with the specified timer interval.
        /// </summary>
        /// <param name="millisecs">The timer's interval in milliseconds.</param>
        public void StartLayout(int millisecs)
        {
            if (millisecs <= 10)
            {
                throw new ApplicationException("The timer value should be bigger than ten milliseconds.");
            }
            layoutTimer.Interval = TimeSpan.FromMilliseconds(millisecs);
            StartLayout();
        }

        /// <summary>
        /// Starts the layout for a specified time span.
        /// </summary>
        /// <param name="duration">The duration during which the layout process will act.</param>
        public void StartLayout(TimeSpan duration)
        {
            var timeWindowTimer = new DispatcherTimer { Interval = duration };
            timeWindowTimer.Tick += (sender, args) =>
                              {
                                  StopLayout();
                                  timeWindowTimer.Stop();
                              };
            StartLayout();
            timeWindowTimer.Start();
        }

        /// <summary>
        /// Stops the layout.
        /// </summary>
        public void StopLayout()
        {
            if (layoutTimer.IsEnabled)
                layoutTimer.Stop();
        }

        /// <summary>
        /// Adds a vertex to the diagram.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <seealso cref="AddEdge">AddEdge</seealso>
        public void AddVertex(Vertex vertex)
        {
            RaiseVertexEvent(vertex, VertexAdd);
            Vertex existingVertex = graph.Vertices.Where(v => v.ID == vertex.ID).FirstOrDefault();
            if (existingVertex != null)
            {
                Debug.WriteLine($"ALREADY ADDED: {vertex.ID}: {vertex.Title}");
            }

            graph.AddVertex(vertex);
        }
        /// <summary>
        /// Adds a vertex and an edge from this vertex to the given target.
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="to"></param>
        public void AddVertex(Vertex vertex, Vertex to)
        {
            if (vertex == null || to == null)
            {
                throw new ArgumentNullException("One of the vertex argument was NULL.");
            }
            AddVertex(vertex);
            AddEdge(vertex, to);
        }

        /// <summary>
        /// Adds an edge between the two given vertices.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public void AddEdge(Vertex from, Vertex to)
        {
            AddEdge(from, to, "");
        }
        public void AddEdge(Vertex from, Vertex to, string label)
        {
            if (from == null || to == null)
            {
                return;
            }

            //if (!HasEdge(from, to))
            graph.AddEdge(from, to, label);
        }

        /// <summary>
        /// Displays the underlying graph. Use this method after data was batch imported in the <see cref="Graph"/>.
        /// </summary>
        internal void DisplayGraph()
        {
            foreach (var n in graph.Vertices)
            {
                AddVertexState(n);
            }

            foreach (var vertexLoc in VertexStates)
            {
                AddVisualVertex(vertexLoc);
            }
            foreach (var edge in graph.Edges)
            {
                CreateVisualForEdge(edge.From, edge.To, "");
            }
        }
        private void AddVisualVertex(KeyValuePair<Vertex, VertexState> vertexLoc)
        {
            var visual = CreateVisualForVertex(vertexLoc.Key);
            vertexLoc.Value.Visual = visual;
            visual.VertexState = vertexLoc.Value;

            /* Set the position of the vertex */
            visual.SetValue(LeftProperty, vertexLoc.Value.Position.X - visual.ActualWidth / 2);
            visual.SetValue(TopProperty, vertexLoc.Value.Position.Y - visual.ActualHeight / 2);

            Children.Add(visual);
        }
        public void Reset()
        {
            graph.Edges.Clear();
            graph.Vertices.Clear();
        }
        public bool HasVertex(string id)
        {
            return graph.Vertices.Where(v => v.ID == id).Count() > 0;
        }
        protected virtual void CreateVisualForEdge(Vertex from, Vertex to, string label)
        {
            //if (HasVisualEdge(from, to)) return;

            var fromState = VertexStates[from];
            var toState = VertexStates[to];

            var a = fromState.Position;
            var b = toState.Position;

            var edge = new VisualEdge
            {
                X1 = a.X,
                X2 = b.X,
                Y1 = a.Y,
                Y2 = b.Y,
                Stroke = LineBrush,
                StrokeThickness = lineThickness,
                Label = label
            };
            if (fromState.ChildrenLines.Count(s => s.Key == to) == 0)
                fromState.ChildrenLines.Add(new KeyValuePair<Vertex, VisualEdge>(to, edge));
            if (toState.ChildrenLines.Count(s => s.Key == from) == 0)
                toState.ParentLines.Add(new KeyValuePair<Vertex, VisualEdge>(from, edge));

            Children.Insert(0, edge); //insert makes sure the edge is underneath all vertices
            if (edge.EdgeLabel != null) Children.Insert(0, edge.EdgeLabel);

            ToolTipService.SetToolTip(edge, new Tip { From = from, To = to });
            //edge.MouseEnter += (sender, args) => { MessageBox.Show("Whatever you wish to display about the connection or vertices"); };

        }

        protected bool HasVisualEdge(Vertex from, Vertex to)
        {
            var fromState = VertexStates[from];
            var toState = VertexStates[to];

            return fromState.ChildrenLines.Count(s => s.Key == to) > 0 || fromState.ParentLines.Count(s => s.Key == to) > 0 || toState.ChildrenLines.Count(s => s.Key == from) > 0 || toState.ParentLines.Count(s => s.Key == from) > 0;
        }


        /// <summary>
        /// Adds a state for the given vertex.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <returns></returns>
        private VertexState AddVertexState(Vertex n)
        {
            if (n == null) return null;
            Point initialPosition;
            if (n.InitialPosition.X != 0 || n.InitialPosition.Y != 0)
                initialPosition = n.InitialPosition;
            else
                initialPosition = new Point(300 + 200 * (rnd.NextDouble() - 0.5), 300 + 150 * (rnd.NextDouble() - 0.5));
            var ns = new VertexState
            {
                Position = initialPosition,
                Velocity = new Point(0, 0),
                Vertex = n
            };
            VertexStates.Add(n, ns);
            return ns;
        }

        /// <summary>
        /// Applies one step of the graph layout algorithm, moving vertices to a more stable configuration.
        /// </summary>
        public void StepLayout()
        {
            for (var i = 0; i < 10; i++)
            {
                Layout();
                if (!dragState.IsDragging && UseCentralForce)
                    MoveDiagramTo(new Point(ActualWidth / 2, ActualHeight / 2), centroidSpeed);
                UpdateVisualPositions();
            }
        }
        private void UpdateVisualPosition(VertexState ns)
        {
            if (double.IsNaN(ns.Position.X) || double.IsNaN(ns.Position.Y))
                return;

            SetLeft(ns.Visual, ns.Position.X - ns.Visual.ActualWidth / 2);
            SetTop(ns.Visual, ns.Position.Y - ns.Visual.ActualHeight / 2);
            //todo: use databinding here
            foreach (var kvp in ns.ChildrenLines)
            {
                var childLoc = VertexStates[kvp.Key].Position;
                kvp.Value.X1 = ns.Position.X;
                kvp.Value.Y1 = ns.Position.Y;
                kvp.Value.X2 = childLoc.X;
                kvp.Value.Y2 = childLoc.Y;
                var p = GetLabelPosition(kvp.Value);
                SetLeft(kvp.Value.EdgeLabel, p.X);
                SetTop(kvp.Value.EdgeLabel, p.Y);
            }

        }

        private static Point GetLabelPosition(VisualEdge edge)
        {
            return new Point(GetHalfWay(edge.X1, edge.X2), GetHalfWay(edge.Y1, edge.Y2));
        }
        private static double GetHalfWay(double a, double b)
        {
            if (double.IsNaN(a) || double.IsNaN(b))
            {
                return 0;
            }
            if (a < b)
                return a + (b - a) / 2;
            return b + (a - b) / 2;
        }

        private void UpdateVisualPositions()
        {
            foreach (var vertexLoc in VertexStates)
            {
                UpdateVisualPosition(vertexLoc.Value);
            }
        }

        /// <summary>
        /// Moves the diagram globally to a given location.
        /// </summary>
        /// <param name="point">The desired centroid.</param>
        /// <param name="speed">The speed with which the visual motion should occur. Around a value of 10 the motion will appear as instantenously, while a value of 0.5 gives a slow-motion effect.</param>
        private void MoveDiagramTo(Point point, double speed)
        {
            var centroid = CenterOfGravity;
            var delta = new Point(point.X - centroid.X, point.Y - centroid.Y);

            var deltaLength = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            if (deltaLength == 0)
                return; //we're at the right spot already
            // If we're very close, don't move at the full speed anymore
            if (speed > deltaLength) speed = 0.5;

            delta.X = (delta.X / deltaLength) * speed;
            delta.Y = (delta.Y / deltaLength) * speed;

            foreach (var state in VertexStates.Values)
            {
                state.Position.X += delta.X;
                state.Position.Y += delta.Y;
            }
        }

        #region Layout algorithm

        /// <summary>
        /// See the <see
        /// cref="http://en.wikipedia.org/wiki/Li%C3%A9nard-Wiechert_Potentials">Lienard-Wiechert
        /// potential</see>.
        /// </summary>
        /// <param name="a">a point</param>
        /// <param name="b">another point</param>
        private static Point RepulsionForce(Point a, Point b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            var sqDist = dx * dx + dy * dy;
            var d = Math.Sqrt(sqDist);
            var repulsion = repulsionStrength * 1.0 / sqDist;
            repulsion += -repulsionStrength * 0.00000006 * d;
            //clip the repulsion
            if (repulsion > repulsionClipping) repulsion = repulsionClipping;
            return new Point(repulsion * (dx / d), repulsion * (dy / d));
        }

        /// <summary>
        /// Calculates the attractions force between two given points.
        /// </summary>
        /// <param name="a">A point in space.</param>
        /// <param name="b">Anoter point in space.</param>
        /// <returns>The force vector.</returns>
        private static Point AttractionForce(Point a, Point b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            var sqDist = dx * dx + dy * dy;
            var d = Math.Sqrt(sqDist);
            var mag = -attractionStrength * 0.001 * Math.Pow(d, 1.20);

            return new Point(mag * (dx / d), mag * (dy / d));
        }

        /// <summary>
        /// The actual spring-embedder algorithm.
        /// </summary>
        /// <returns></returns>
        protected virtual void Layout()
        {

            foreach (var kvp in VertexStates)
            {
                if (dragState.IsDragging && dragState.VertexBeingDragged == kvp.Value) continue;
                if (kvp.Value.Vertex.IsFixed) continue;

                var n = kvp.Key;
                var state = kvp.Value;

                var f = new Point(0, 0); // Force
                //compute the repulsion on this vertex, with respect to ALL vertices
                foreach (var coulomb in from kvpB in VertexStates where kvpB.Key != n select RepulsionForce(state.Position, kvpB.Value.Position))
                {
                    f.X += coulomb.X;
                    f.Y += coulomb.Y;
                }
                //compute the attraction on this vertex, only to the adjacent vertices
                foreach (var child in graph.AdjacentVertices(n))
                {
                    var hooke = AttractionForce(state.Position, VertexStates[child].Position);
                    f.X += hooke.X;
                    f.Y += hooke.Y;
                }

                var v = state.Velocity;

                state.Velocity = new Point((v.X + timeStep * f.X) * damping, (v.Y + timeStep * f.Y) * damping);

                state.Position.X += timeStep * state.Velocity.X;
                state.Position.Y += timeStep * state.Velocity.Y;
            }

        }

        #endregion

        #region VisualVertex creation

        /// <summary>
        /// Creates the visual representation for a given vertex.
        /// </summary>
        /// <param name="n">The vertex for which a visual will be created.</param>
        /// <returns></returns>
        private VisualVertex CreateVisualForVertex(Vertex n)
        {
            var visualVertex = new VisualVertex { BorderBrush = vertexDefaultBorder, Title = n.Title, Info = n.Info, Id = n.ID };
            switch (n.Type)
            {
                case VertexType.Standard:
                    //use the normal template mechanism
                    visualVertex.Style = TryFindResource(n.Type.ToString()) as Style;
                    break;
                case VertexType.Bubble:
                case VertexType.Person:
                case VertexType.Idea:
                    visualVertex.Style = TryFindResource(n.Type.ToString()) as Style;
                    break;
                case VertexType.NotSpecified:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("n");
            }

            if (Application.Current != null)
            {
                visualVertex.MouseLeftButtonDown += VisualVertex_MouseLeftButtonDown;
                visualVertex.MouseEnter += VisualVertex_MouseEnter;
                visualVertex.MouseLeave += VisualVertex_MouseLeave;
                visualVertex.MouseDoubleClick += VisualVertex_MouseDoubleClick;
                visualVertex.DataContext = n.Info;
            }

            return visualVertex;
        }

        private void VisualVertex_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //RaiseVertexEvent(Vertex.Empty, VertexDoubleClick);
            var visualVertex = sender as VisualVertex;
            var n = VertexStates.Single(s => s.Value.Visual == visualVertex).Key;
            var arg = RaiseVertexEvent(n, VertexDoubleClick);
        }

        void VisualVertex_MouseLeave(object sender, MouseEventArgs e)
        {
            if (dragState.IsDragging) return;
            var visualVertex = sender as VisualVertex;

            if (visualVertex == null)
                return;
            LowlightVertex(visualVertex);

            HighlightChildrenReset(visualVertex);
            HighlightParentsReset(visualVertex);
            RaiseVertexEvent(Vertex.Empty, VertexHovering);

        }

        void VisualVertex_MouseEnter(object sender, MouseEventArgs e)
        {
            if (dragState.IsDragging) return;
            var visualVertex = sender as VisualVertex;
            if (visualVertex == null)
                return;
            HighlightVertex(visualVertex);
            HighlightChildren(visualVertex);
            HighlightParents(visualVertex);
            RaiseVertexEvent(visualVertex.VertexState.Vertex, VertexHovering);
        }

        /// <summary>
        /// Highlights the given vertex.
        /// </summary>
        /// <param name="visualVertex">The visual vertex.</param>
        private void HighlightVertex(VisualVertex visualVertex)
        {
            visualVertex.Inflate();
            visualVertex.BorderBrush = vertexHighlightBorder;
        }
        private void LowlightVertex(VisualVertex visualVertex)
        {
            visualVertex.Deflate();
            if (visualVertex.VertexState.Vertex == Selected1 || visualVertex.VertexState.Vertex == Selected2)
                visualVertex.BorderBrush = vertexSelectedBorder;
            else
                visualVertex.BorderBrush = vertexDefaultBorder;
        }
        /// <summary>
        /// Highlights the vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        public void HighlightVertex(Vertex vertex)
        {
            if (vertex == null) return;
            if (VertexStates.Keys.Contains(vertex))
            {
                HighlightVertex(VertexStates[vertex].Visual);
            }
        }
        /// <summary>
        /// Lowlights the given vertex.
        /// </summary>
        /// <seealso cref="HighlightVertex"/>
        /// <param name="vertex">The vertex.</param>
        public void LowlightVertex(Vertex vertex)
        {
            if (vertex == null) return;
            if (VertexStates.Keys.Contains(vertex))
            {
                LowlightVertex(VertexStates[vertex].Visual);
            }
        }

        internal void HighlightParentsReset(VisualVertex visualVertex)
        {
            foreach (var line in visualVertex.VertexState.ParentLines)
            {
                LowlightVertex(line.Key);
                line.Value.Stroke = LineBrush;
                line.Value.StrokeThickness = lineThickness;
            }
        }
        internal void HighlightChildrenReset(VisualVertex visualVertex)
        {
            foreach (var line in visualVertex.VertexState.ChildrenLines)
            {
                LowlightVertex(line.Key);
                line.Value.Stroke = LineBrush;
                line.Value.StrokeThickness = lineThickness;
            }
        }

        internal void HighlightParents(VisualVertex visualVertex)
        {
            foreach (var line in visualVertex.VertexState.ParentLines)
            {
                HighlightVertex(line.Key);
                line.Value.Stroke = vertexHighlightBorder;
                line.Value.StrokeThickness = lineHighlightThickness;
            }
        }

        internal void HighlightChildren(VisualVertex visualVertex)
        {
            foreach (var line in visualVertex.VertexState.ChildrenLines)
            {
                HighlightVertex(line.Key);
                line.Value.Stroke = vertexHighlightBorder;
                line.Value.StrokeThickness = lineHighlightThickness;
            }
        }
        /// <summary>
        /// Highlights the children vertices of the given vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        public void HighlightChildren(Vertex vertex)
        {
            if (vertex == null) return;
            if (VertexStates.Keys.Contains(vertex))
                HighlightChildren(VertexStates[vertex].Visual);
        }
        /// <summary>
        /// Highlights the parent vertices of the given vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        public void HighlightParents(Vertex vertex)
        {
            if (vertex == null) return;
            if (VertexStates.Keys.Contains(vertex))
                HighlightParents(VertexStates[vertex].Visual);
        }
        /// <summary>
        /// Handles the MouseLeftButtonDown event of the VisualVertex control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        void VisualVertex_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ReadOnly) return;

            var visualVertex = sender as VisualVertex;
            var n = VertexStates.Single(s => s.Value.Visual == visualVertex).Key;
           
            var arg = RaiseVertexEvent(n, VertexClick);
            if (arg != null && arg.Handled)
            {
                e.Handled = true;
                return;
            }
            if (n.IsFixed) return; //do not move the fixed vertices

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (Selected1 == null)
                {
                    Selected1 = n;
                    RaiseVertexEvent(n, VertexSelect1);
                    return;
                }
                if (Selected2 == null)
                {
                    Selected2 = n;
                    RaiseVertexEvent(n, VertexSelect2);
                    return;
                }
                //neither are null
                VertexStates[Selected1].Visual.BorderBrush = vertexDefaultBorder;
                VertexStates[Selected2].Visual.BorderBrush = vertexDefaultBorder;

                Selected1 = n;
                Selected2 = null;
                RaiseVertexEvent(n, VertexSelect1);

                return;
            }

            if (visualVertex == null)
                return;

            dragState.VertexBeingDragged = visualVertex.VertexState;
            dragState.OffsetWithinVertex = e.GetPosition(visualVertex);
            dragState.IsDragging = true;
        }
        /// <summary>
        /// Deletes the given vertex from the diagram and removes all edges linked to this vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        public void DeleteVertex(Vertex vertex)
        {
            if (!IsHomeDeletionEnabled && vertex.Equals(Home))
                throw new ApplicationException("The home vertex cannot be deleted.");

            graph.RemoveVertex(vertex);
        }
        /// <summary>
        /// Deletes the edge between the two given vertices.
        /// </summary>
        public void DeleteEdge(Edge edge)
        {

        }
        /// <summary>
        /// Deletes the edge between the two given vertices.
        /// </summary>
        /// <param name="from">From vertex.</param>
        /// <param name="to">To vertex.</param>
        public void DeleteEdge(Vertex from, Vertex to)
        {
            graph.RemoveEdge(from, to);
        }

        #endregion
        #region Reading files
        /// <summary>
        /// Some file checks before load.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private bool FilePathIsOK(string path)
        {
            //if (!File.Exists(path))
            //    return false;
            ////anything else?
            return true;
        }



        /// <summary>
        /// Creates a graph by interpreting a specially formatted text file.
        /// </summary>
        /// <returns>The graph created.</returns>
        internal void BuildGraph(Stream s)
        {
            var graphLines = Utils.ReadAllLines(s);
            BuildGraph(graphLines);
        }
        internal void BuildGraph(XDocument xdoc)
        {
            if (xdoc == null) return;
            if (xdoc.Root.Name != "Graph") return;
            AddVertices(xdoc);

        }

        private void AddVertices(XDocument xdoc)
        {
            var memory = new Dictionary<string, Vertex>();
            foreach (var element in xdoc.Root.Elements("Vertex"))
            {
                try
                {
                    var n = new Vertex();
                    if (element.Attributes("id").Count() == 0)
                    {
                        continue; //ID is mendatory
                    }
                    n.ID = element.Attribute("id").Value;
                    if (element.Attributes("Info").Count() > 0)
                    {
                        n.Info = element.Attribute("Info").Value;
                    }
                    if (element.Attributes("Title").Count() > 0)
                    {
                        n.Title = element.Attribute("Title").Value;
                    }
                    memory.Add(n.ID, n);
                    graph.AddVertex(n);
                }
                catch
                {
                    continue;
                }
            }
            foreach (var element in xdoc.Root.Elements("Vertex"))
            {
                try
                {
                    if (element.Attributes("Links").Count() > 0)
                    {
                        var links = element.Attribute("Links").Value.Split(',');
                        var id = element.Attribute("id").Value;
                        foreach (var s in links)
                        {

                            graph.AddEdge(memory[id], memory[s]);
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

        }

        /// <summary>
        /// Builds a graph represented by the string array encoding.
        /// </summary>
        /// <param name="elements">String array from the graph API.</param>
        internal void BuildGraph(string[] elements)
        {
            //BlockEvent = true;
            var vertexDict = new Dictionary<long, Vertex>();

            // First pass, get all the vertices
            foreach (var vertex in elements)
            {
                var vertexData = vertex.Split('/');

                if (vertexData.Length < 2) continue;

                var n = new Vertex
                {
                    Title = vertexData[1]
                };

                if (vertexData.Length <= 4)
                {
                    n.Type = VertexType.Standard;
                }
                else if (vertexData[4] == "Standard")
                {
                    n.Type = VertexType.Standard;
                }
                else
                {
                    n.Type = VertexType.Bubble;
                }

                vertexDict.Add(long.Parse(vertexData[0]), n);
                graph.AddVertex(n);
            }

            // Second pass, get the edges
            foreach (var vertex in elements)
            {
                var vertexData = vertex.Split('/');

                var vertexId = long.Parse(vertexData[0]);

                if (vertexData.Length > 2)
                {
                    var vertexChildren = vertexData[2].Split(',');
                    foreach (var child in vertexChildren)
                    {
                        if (child.Length == 0) continue;
                        graph.AddEdge(vertexDict[vertexId], vertexDict[long.Parse(child)]);
                    }
                }
            }
            //BlockEvent = false;

        }

        /// <summary>
        /// For testing purposes: Builds a random graph with N vertices.
        /// </summary>
        /// <param name="N">The N.</param>
        /// <returns>
        /// The random graph as represented by the local graph library.
        /// </returns>
        void CreateRandomGraph(int N)
        {
            return;

            Graph g = new Graph();

            List<Vertex> allVertices = new List<Vertex>();

            /* Create N vertices */
            for (int i = 0; i < N; i++)
            {
                Vertex n = new Vertex { Type = VertexType.Standard, Title = i.ToString() };
                allVertices.Add(n);
                g.AddVertex(n);
            }


            Random r = new Random();
            for (int i = 0; i < N; i++)
            {
                int dest = r.Next(allVertices.Count);
                if (dest == i) continue;

                g.AddEdge(allVertices[i], allVertices[dest]);
            }

            return;
        }

        #endregion
        #region Canvas interaction
        /// <summary>
        /// Handles the MouseLeave event of the Canvas control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            dragState.IsDragging = false;
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the Canvas control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragState.IsDragging = false;
        }

        /// <summary>
        /// Handles the MouseMove event of the Canvas control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragState.IsDragging)
            {
                var position = e.GetPosition(dragState.VertexBeingDragged.Visual);
                position.X += (-dragState.OffsetWithinVertex.X);
                position.Y += (-dragState.OffsetWithinVertex.Y);

                var ns = dragState.VertexBeingDragged;

                ns.Position.X += position.X;
                ns.Position.Y += position.Y;

                SetLeft(ns.Visual, ns.Position.X - ns.Visual.ActualWidth / 2);
                SetTop(ns.Visual, ns.Position.Y - ns.Visual.ActualHeight / 2);
                if (!layoutTimer.IsEnabled)
                {
                    UpdateVisualPositions();
                }
            }
        }
        #endregion


        /// <summary>
        /// Adds a new edge between the <see cref="Selected1"/> and <see cref="Selected2"/> vertices.
        /// </summary>
        public void AddEdgeToSelected()
        {
            if (Selected1 == null || Selected2 == null)
            {
                return;
            }

            AddEdge(Selected1, Selected2);
            //if (!HasEdge(Selected1, Selected2))
            //{
            //    AddEdge(Selected1, Selected2);
            //}
        }
        #endregion
    }

    public sealed class Tip
    {
        public Vertex From { get; set; }
        public Vertex To { get; set; }
    }
}
