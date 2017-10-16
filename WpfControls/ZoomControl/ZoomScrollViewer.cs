using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfControls.Enum;

namespace WpfControls.ZoomControl
{
    public class ZoomScrollViewer : ScrollViewer
    {
        #region Fields

        protected bool _manipulationStarted;
        protected FrameworkElement _content;

        #endregion

        #region Dependency properties

        public static readonly DependencyProperty ZoomFactorProperty = DependencyProperty
            .Register("ZoomFactor", typeof(double), typeof(ZoomScrollViewer), new PropertyMetadata(1.0, new PropertyChangedCallback(ZoomFactorCallBack)));

        public static readonly DependencyProperty MinZoomFactorProperty = DependencyProperty
            .Register("MinZoomFactor", typeof(double), typeof(ZoomScrollViewer), new PropertyMetadata(1.0, new PropertyChangedCallback(MinZoomFactorCallBack)));

        public static readonly DependencyProperty MaxZoomFactorProperty = DependencyProperty
            .Register("MaxZoomFactor", typeof(double), typeof(ZoomScrollViewer), new PropertyMetadata(1.0, new PropertyChangedCallback(MaxZoomFactorCallBack)));

        public static readonly DependencyProperty ZoomModeProperty = DependencyProperty
            .Register("ZoomMode", typeof(ZoomMode), typeof(ZoomScrollViewer), new PropertyMetadata(ZoomMode.Disabled));

        #endregion

        #region Properties

        public ZoomMode ZoomMode
        {
            get { return (ZoomMode)GetValue(ZoomModeProperty); }
            set { SetValue(ZoomModeProperty, value); }
        }

        public double MaxZoomFactor
        {
            get { return (double)GetValue(MaxZoomFactorProperty); }
            set { SetValue(MaxZoomFactorProperty, value); }
        }

        public double MinZoomFactor
        {
            get { return (double)GetValue(MinZoomFactorProperty); }
            set { SetValue(MinZoomFactorProperty, value); }
        }

        public double ZoomFactor
        {
            get { return (double)GetValue(ZoomFactorProperty); }
            set { SetValue(ZoomFactorProperty, value); }
        }

        #endregion

        #region Constructors

        public ZoomScrollViewer()
        {
            Loaded += ZoomScrollViewer_Loaded;
        }

        private void ZoomScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            _content = Content as FrameworkElement;
        }

        #endregion

        #region Callbacks

        private static void ZoomFactorCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = d as ZoomScrollViewer;
            var content = scrollViewer.Content as FrameworkElement;

            var zoomFactor = 0.0;

            if (scrollViewer != null && scrollViewer.ZoomMode == ZoomMode.Enabled && content != null)
            {
                var newValue = (double)e.NewValue;

                zoomFactor = newValue > scrollViewer.MaxZoomFactor
                 ? scrollViewer.MaxZoomFactor
                 : newValue < scrollViewer.MinZoomFactor
                    ? scrollViewer.MinZoomFactor
                    : newValue;
            }

            if (!scrollViewer._manipulationStarted && zoomFactor > 0.0)
            {
                var matrix = content.LayoutTransform.Value;
                zoomFactor = zoomFactor / matrix.M11;
                matrix.Scale(zoomFactor, zoomFactor);

                content.LayoutTransform = new MatrixTransform(matrix);
            }
        }

        private static void MinZoomFactorCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = d as ZoomScrollViewer;
            if (scrollViewer != null && scrollViewer.ZoomMode == ZoomMode.Enabled)
            {
                var newValue = (double)e.NewValue;
                if (newValue > scrollViewer.ZoomFactor) scrollViewer.ZoomFactor = newValue;
            }
        }

        private static void MaxZoomFactorCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = d as ZoomScrollViewer;
            if (scrollViewer != null && scrollViewer.ZoomMode == ZoomMode.Enabled)
            {
                var newValue = (double)e.NewValue;
                if (newValue < scrollViewer.ZoomFactor) scrollViewer.ZoomFactor = newValue;
            }
        }

        #endregion

        #region Methods override

        protected override void OnManipulationStarting(ManipulationStartingEventArgs e)
        {
            if (_content == null) return;

            _manipulationStarted = true;
            e.ManipulationContainer = _content;

            base.OnManipulationStarting(e);

            switch (ZoomMode)
            {
                case ZoomMode.Enabled:
                    e.Mode |= ManipulationModes.Scale;
                    break;

                case ZoomMode.Disabled:
                    e.Mode &= ~ManipulationModes.Scale;
                    break;
            }

            e.Handled = true;
        }

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            if (_content == null) return;

            if (e.CumulativeManipulation.Scale.X == 1 && e.CumulativeManipulation.Scale.Y == 1)
            {
                // TODO: bubbling event if if translation results with no effect
                //if (HorizontalOffset < 1 && e.DeltaManipulation.Translation.X > 0)
                //{
                //    e.Handled = false;
                //    return;
                //}

                base.OnManipulationDelta(e);
            }

            var matrix = _content.LayoutTransform.Value;
            var scale = e.DeltaManipulation.Scale;
            var origin = e.ManipulationOrigin;
            var diffX = ScrollableWidth - ViewportWidth;
            var diffY = ScrollableHeight - ViewportHeight;

            if (e.CumulativeManipulation.Scale.X != 1 || e.CumulativeManipulation.Scale.Y != 1)
            {
                var newScale = matrix.M11 * scale.X;

                if (newScale >= MinZoomFactor && newScale <= MaxZoomFactor)
                {
                    matrix.ScaleAt(scale.X, scale.Y, origin.X, origin.Y);

                    if (matrix.OffsetX > 0)
                        matrix.OffsetX = 0;
                    if (matrix.OffsetY > 0)
                        matrix.OffsetY = 0;

                    _content.LayoutTransform = new MatrixTransform(matrix);

                    ScrollToHorizontalOffset(-matrix.OffsetX);
                    ScrollToVerticalOffset(-matrix.OffsetY);

                    ZoomFactor = newScale;
                }
                else
                {
                    e.Handled = false;
                    return;
                }
            }
            else
            {
                matrix.OffsetX = -HorizontalOffset;
                matrix.OffsetY = -VerticalOffset;

                _content.LayoutTransform = new MatrixTransform(matrix);
            }

            e.Handled = true;
        }

        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            _manipulationStarted = false;
            base.OnManipulationCompleted(e);
            e.Handled = false;
        }

        // Override is optional to remove unnecessary behavior
        protected override void OnManipulationBoundaryFeedback(ManipulationBoundaryFeedbackEventArgs e)
        {
            // uncomment this to use base class implementation
            //base.OnManipulationBoundaryFeedback(e);
            e.Handled = true;
        }


        // TODO: handle mouse zoom

        //protected double _scaleRate = 1.1;
        //protected Vector _currentScale = new Vector(1, 1);

        //protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        //{
        //    if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        //    {
        //        base.OnPreviewMouseWheel(e);
        //        return;
        //    }

        //    if (_content == null) return;

        //    var matrix = _content.LayoutTransform.Value;
        //    var origin = e.GetPosition(_content);

        //    var newScale = _currentScale.X;

        //    if (e.Delta > 0 && newScale <= MaxZoomFactor)
        //    {
        //        _currentScale.X *= _scaleRate;
        //        _currentScale.Y *= _scaleRate;
        //    }
        //    else if (newScale >= MinZoomFactor)
        //    {
        //        _currentScale.X /= _scaleRate;
        //        _currentScale.Y /= _scaleRate;
        //    }
        //    else return;

        //    newScale = _currentScale.X;

        //    if (newScale >= MinZoomFactor && newScale <= MaxZoomFactor)
        //    {
        //        matrix.ScaleAt(newScale, newScale, origin.X, origin.Y);

        //        var diffX = ScrollableWidth - ViewportWidth;
        //        var diffY = ScrollableHeight - ViewportHeight;

        //        if (matrix.OffsetX > 0)
        //            matrix.OffsetX = 0;
        //        if (matrix.OffsetY > 0)
        //            matrix.OffsetY = 0;

        //        if (-matrix.OffsetX > diffX)
        //            matrix.OffsetX = -diffX;
        //        if (-matrix.OffsetY > diffY)
        //            matrix.OffsetY = -diffY;


        //        //await Application.Current.Dispatcher.InvokeAsync(() =>
        //        //{
        //        _content.LayoutTransform = new MatrixTransform(matrix);

        //        ScrollToHorizontalOffset(-matrix.OffsetX);
        //        ScrollToVerticalOffset(-matrix.OffsetY);

        //        ZoomFactor = newScale;
        //        //}, System.Windows.Threading.DispatcherPriority.Send);

        //    }

        //    e.Handled = true;
        //}

        //protected override void OnMouseWheel(MouseWheelEventArgs e)
        //{
        //    if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        //        base.OnMouseWheel(e);

        //    e.Handled = true;
        //}

        #endregion
    }
}
