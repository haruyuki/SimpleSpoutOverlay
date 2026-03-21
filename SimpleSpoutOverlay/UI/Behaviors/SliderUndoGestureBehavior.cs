using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SimpleSpoutOverlay.UI.ViewModels;

namespace SimpleSpoutOverlay.UI.Behaviors
{
    /// Attached behavior that wires slider drag start/complete events to undo gesture lifecycle
    /// on the MainWindowViewModel.
    public static class SliderUndoGestureBehavior
    {
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(SliderUndoGestureBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Slider slider)
                return;

            bool isEnabled = (bool)e.NewValue;
            if (isEnabled)
            {
                slider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnSliderDragStarted));
                slider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnSliderDragCompleted));
            }
            else
            {
                slider.RemoveHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnSliderDragStarted));
                slider.RemoveHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnSliderDragCompleted));
            }
        }

        private static void OnSliderDragStarted(object sender, DragStartedEventArgs e)
        {
            if (sender is Slider { DataContext: MainWindowViewModel viewModel })
            {
                viewModel.BeginSliderUndoGesture();
            }
        }

        private static void OnSliderDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (sender is not Slider { DataContext: MainWindowViewModel viewModel })
                return;

            if (e.Canceled)
            {
                viewModel.CancelSliderUndoGesture();
            }
            else
            {
                viewModel.CommitSliderUndoGesture();
            }
        }
    }
}


