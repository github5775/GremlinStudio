using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Graphyte
{
    /// <summary>
    /// The visual representation of a <see cref="Vertex"/>.
    /// </summary>
    public sealed class VisualVertex : Control
    {

        #region Fields
        private const double InflateRatio = 1.5D;
        internal VertexState VertexState { get; set; }
        #endregion

        #region Title dependency property

        /// <summary>
        /// Title Dependency Property
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(VisualVertex));

        /// <summary>
        /// Gets or sets the Title property.  
        /// </summary>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public string Id { get; set; }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualVertex"/> class.
        /// </summary>
        public VisualVertex()
        {
            DefaultStyleKey = typeof(VisualVertex);
        }

        #region Info

        /// <summary>
        /// Info Dependency Property
        /// </summary>
        public static readonly DependencyProperty InfoProperty =
            DependencyProperty.Register("Info", typeof(object), typeof(VisualVertex), null);

        /// <summary>
        /// Gets or sets the Info property.  This dependency property 
        /// indicates ....
        /// </summary>
        public object Info
        {
            get { return GetValue(InfoProperty); }
            set { SetValue(InfoProperty, value); }
        }

        #endregion

        #region Methods
        /// <summary>
        /// Inflates the vertex.
        /// </summary>
        public void Inflate()
        {
            RenderTransform = new ScaleTransform();
            RenderTransformOrigin = new Point(0, 0);
            (RenderTransform as ScaleTransform).AnimateTo(300, InflateRatio, InflateRatio, null);
        }
        /// <summary>
        /// Deflates the vertex.
        /// </summary>
        public void Deflate()
        {
            RenderTransform = new ScaleTransform();
            RenderTransformOrigin = new Point(0, 0);
            (RenderTransform as ScaleTransform).AnimateTo(300, 1D, 1D, null);
        }
        #endregion




    }

}