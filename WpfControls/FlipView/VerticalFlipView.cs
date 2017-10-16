using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WpfControls.Factories;

namespace WpfControls.FlipView
{
    public class VerticalFlipView : Selector
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
        static VerticalFlipView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VerticalFlipView), new FrameworkPropertyMetadata(typeof(VerticalFlipView)));
            SelectedIndexProperty.OverrideMetadata(typeof(VerticalFlipView), new FrameworkPropertyMetadata(-1, OnSelectedIndexChanged));
        }

        public VerticalFlipView()
        {
            this.CommandBindings.Add(new CommandBinding(NextCommand, this.OnNextExecuted, this.OnNextCanExecute));
            this.CommandBindings.Add(new CommandBinding(PreviousCommand, this.OnPreviousExecuted, this.OnPreviousCanExecute));
        }
        #endregion

        #region Private methods
        private void OnRootManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            this.fromValue = e.TotalManipulation.Translation.Y;
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
                this.RunSlideAnimation(0, (this.PART_Root.RenderTransform as MatrixTransform)?.Matrix.OffsetY ?? 0.0);
            }
            this.elasticFactor = 1.0;
        }

        private void OnRootManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (!(this.PART_Root.RenderTransform is MatrixTransform))
            {
                this.PART_Root.RenderTransform = new MatrixTransform();
            }

            Matrix matrix = ((MatrixTransform)this.PART_Root.RenderTransform).Matrix;
            var delta = e.DeltaManipulation;

            if ((this.SelectedIndex == 0 && delta.Translation.Y > 0 && this.elasticFactor > 0)
                || (this.SelectedIndex == this.Items.Count - 1 && delta.Translation.Y < 0 && this.elasticFactor > 0))
            {
                this.elasticFactor -= 0.05;
            }

            matrix.Translate(0, delta.Translation.Y * elasticFactor);
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
            var control = d as VerticalFlipView;

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
                double toValue = (int)e.OldValue < (int)e.NewValue ? -this.ActualHeight : this.ActualHeight;
                this.RunSlideAnimation(toValue, fromValue);
            }
        }

        private void RefreshViewPort(int selectedIndex)
        {
            if (!this.EnsureTemplateParts())
            {
                return;
            }

            Canvas.SetTop(this.PART_PreviousItem, -this.ActualHeight);
            Canvas.SetTop(this.PART_NextItem, this.ActualHeight);
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

            var story = AnimationFactory.Instance.GetAnimation(this.PART_Root, toValue, fromValue, Orientation.Vertical);
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

        public static RoutedUICommand NextCommand = new RoutedUICommand("Next", "Next", typeof(VerticalFlipView));
        public static RoutedUICommand PreviousCommand = new RoutedUICommand("Previous", "Previous", typeof(VerticalFlipView));

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
