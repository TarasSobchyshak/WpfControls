using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace WpfControls.Factories
{
    public class AnimationFactory
    {
        public static AnimationFactory Instance
        {
            get
            {
                return new AnimationFactory();
            }
        }

        protected AnimationFactory() { }

        public Storyboard GetAnimation(DependencyObject target, double to, double from, Orientation orientation)
        {
            Storyboard story = new Storyboard();
            Storyboard.SetTargetProperty(story, new PropertyPath($"(TextBlock.RenderTransform).(TranslateTransform.{(orientation == Orientation.Horizontal ? "X" : "Y")})"));
            Storyboard.SetTarget(story, target);

            var doubleAnimation = new DoubleAnimationUsingKeyFrames();

            EasingDoubleKeyFrame fromFrame = new EasingDoubleKeyFrame(from);
            fromFrame.EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut };
            fromFrame.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0));

            EasingDoubleKeyFrame toFrame = new EasingDoubleKeyFrame(to);
            toFrame.EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut };
            toFrame.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200));

            doubleAnimation.KeyFrames.Add(fromFrame);
            doubleAnimation.KeyFrames.Add(toFrame);
            story.Children.Add(doubleAnimation);

            return story;
        }
    }
}
