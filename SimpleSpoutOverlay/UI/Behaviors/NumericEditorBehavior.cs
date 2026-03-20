using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace SimpleSpoutOverlay.UI.Behaviors
{
    /// <summary>
    /// Attached behavior that wires numeric textbox interactions:
    /// - Select all text on focus
    /// - Commit binding on Enter key
    /// </summary>
    public static class NumericEditorBehavior
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
                typeof(NumericEditorBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox)
                return;

            bool isEnabled = (bool)e.NewValue;
            if (isEnabled)
            {
                textBox.GotKeyboardFocus += OnTextBoxGotKeyboardFocus;
                textBox.KeyDown += OnTextBoxKeyDown;
            }
            else
            {
                textBox.GotKeyboardFocus -= OnTextBoxGotKeyboardFocus;
                textBox.KeyDown -= OnTextBoxKeyDown;
            }
        }

        private static void OnTextBoxGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Defer SelectAll until after focus chain settles to prevent other focus events from clearing selection
                textBox.Dispatcher.BeginInvoke(() => textBox.SelectAll(), DispatcherPriority.Input);
            }
        }

        private static void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || sender is not TextBox textBox)
            {
                return;
            }

            BindingExpression? expression = textBox.GetBindingExpression(TextBox.TextProperty);
            expression?.UpdateSource();
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }
}

