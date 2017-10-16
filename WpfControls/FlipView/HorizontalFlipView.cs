using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WpfControls.Factories;

namespace WpfControls.FlipView
{
    public class HorizontalFlipView : Selector
    {
        #region Private Fields
        private ContentControl PART_CurrentItem;
        private ContentControl PART_PreviousItem;
        private ContentControl PART_NextItem;
        private FrameworkElement PART_Root;
        private FrameworkElement PART_Container;
        private double fromValue = 0.0;
        private double elasticFactor = 1.0;
        #endregion

        #region Constructor
        static HorizontalFlipView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HorizontalFlipView), new FrameworkPropertyMetadata(typeof(HorizontalFlipView)));
            SelectedIndexProperty.OverrideMetadata(typeof(HorizontalFlipView), new FrameworkPropertyMetadata(-1, OnSelectedIndexChanged));
        }

        public HorizontalFlipView()
        {
            this.CommandBindings.Add(new CommandBinding(NextCommand, this.OnNextExecuted, this.OnNextCanExecute));
            this.CommandBindings.Add(new CommandBinding(PreviousCommand, this.OnPreviousExecuted, this.OnPreviousCanExecute));
        }
        #endregion

        #region Private methods
        private void OnRootManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            this.fromValue = e.TotalManipulation.Translation.X;
            if (fromValue == 0) return;

            if (this.fromValue > 0)
            {
                if (this.SelectedIndex > 0)
                {
                    this.SelectedIndex -= 1;
                }
            }
            else
            {
                if (this.SelectedIndex < this.Items.Count - 1)
                {
                    this.SelectedIndex += 1;
                }
            }

            if (this.elasticFactor < 1)
            {
                this.RunSlideAnimation(0, (this.PART_Root.RenderTransform as MatrixTransform)?.Matrix.OffsetX ?? 0.0);
            }
            this.elasticFactor = 1.0;

            e.Handled = true;
        }

        private void OnRootManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (e.CumulativeManipulation.Scale.X != 1 || e.CumulativeManipulation.Scale.Y != 1)
                return;

            if (!(this.PART_Root.RenderTransform is MatrixTransform))
            {
                this.PART_Root.RenderTransform = new MatrixTransform();
            }

            Matrix matrix = ((MatrixTransform)this.PART_Root.RenderTransform).Matrix;
            var delta = e.DeltaManipulation;

            if ((this.SelectedIndex == 0 && delta.Translation.X > 0 && this.elasticFactor > 0)
                || (this.SelectedIndex == this.Items.Count - 1 && delta.Translation.X < 0 && this.elasticFactor > 0))
            {
                this.elasticFactor -= 0.05;
            }

            matrix.Translate(delta.Translation.X * elasticFactor, 0);
            this.PART_Root.RenderTransform = new MatrixTransform(matrix);

            e.Handled = true;
        }

        private void OnRootManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = this.PART_Container;
            e.Handled = true;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.RefreshViewPort(this.SelectedIndex);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.SelectedIndex > -1)
            {
                this.RefreshViewPort(this.SelectedIndex);
            }
        }
        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as HorizontalFlipView;

            control.OnSelectedIndexChanged(e);
        }

        private void OnSelectedIndexChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!this.EnsureTemplateParts())
            {
                return;
            }

            if ((int)e.NewValue >= 0 && (int)e.NewValue < this.Items.Count)
            {
                double toValue = (int)e.OldValue < (int)e.NewValue ? -this.ActualWidth : this.ActualWidth;
                this.RunSlideAnimation(toValue, fromValue);
            }
        }

        private void RefreshViewPort(int selectedIndex)
        {
            if (!this.EnsureTemplateParts())
            {
                return;
            }

            Canvas.SetLeft(this.PART_PreviousItem, -this.ActualWidth);
            Canvas.SetLeft(this.PART_NextItem, this.ActualWidth);
            this.PART_Root.RenderTransform = new TranslateTransform();

            var currentItem = this.GetItemAt(selectedIndex);
            var nextItem = this.GetItemAt(selectedIndex + 1);
            var previousItem = this.GetItemAt(selectedIndex - 1);

            this.PART_CurrentItem.Content = currentItem;
            this.PART_NextItem.Content = nextItem;
            this.PART_PreviousItem.Content = previousItem;
        }

        public void RunSlideAnimation(double toValue, double fromValue = 0)
        {
            if (!(this.PART_Root.RenderTransform is TranslateTransform))
            {
                this.PART_Root.RenderTransform = new TranslateTransform();
            }

            var story = AnimationFactory.Instance.GetAnimation(this.PART_Root, toValue, fromValue, Orientation.Horizontal);
            story.Completed += (s, e) =>
            {
                this.RefreshViewPort(this.SelectedIndex);
            };
            story.Begin();
        }

        private object GetItemAt(int index)
        {
            if (index < 0 || index >= this.Items.Count)
            {
                return null;
            }

            return this.Items[index];
        }

        private bool EnsureTemplateParts()
        {
            return this.PART_CurrentItem != null &&
                this.PART_NextItem != null &&
                this.PART_PreviousItem != null &&
                this.PART_Root != null;
        }

        private void OnPreviousCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.SelectedIndex > 0;
        }

        private void OnPreviousExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.SelectedIndex -= 1;
        }

        private void OnNextCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.SelectedIndex < (this.Items.Count - 1);
        }

        private void OnNextExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.SelectedIndex += 1;
        }
        #endregion

        #region Commands

        public static RoutedUICommand NextCommand = new RoutedUICommand("Next", "Next", typeof(HorizontalFlipView));
        public static RoutedUICommand PreviousCommand = new RoutedUICommand("Previous", "Previous", typeof(HorizontalFlipView));

        #endregion

        #region Override methods
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.PART_PreviousItem = this.GetTemplateChild("PART_PreviousItem") as ContentControl;
            this.PART_NextItem = this.GetTemplateChild("PART_NextItem") as ContentControl;
            this.PART_CurrentItem = this.GetTemplateChild("PART_CurrentItem") as ContentControl;
            this.PART_Root = this.GetTemplateChild("PART_Root") as FrameworkElement;
            this.PART_Container = this.GetTemplateChild("PART_Container") as FrameworkElement;

            this.Loaded += this.OnLoaded;
            this.SizeChanged += this.OnSizeChanged;
            this.PART_Root.ManipulationStarting += this.OnRootManipulationStarting;
            this.PART_Root.ManipulationDelta += this.OnRootManipulationDelta;
            this.PART_Root.ManipulationCompleted += this.OnRootManipulationCompleted;
        }
        #endregion

    }
}
