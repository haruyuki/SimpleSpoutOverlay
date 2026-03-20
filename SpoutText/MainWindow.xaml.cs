using System.Windows;
using SpoutText.UI.Dialogs;
using SpoutText.UI.ViewModels;

namespace SpoutText;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Dispose();
        }
    }

    private void OnChooseFillColor(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        ColorPickerWindow dialog = new(viewModel.SelectedFillColor)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            viewModel.SelectedFillColor = dialog.SelectedColor;
        }
    }

    private void OnChooseOutlineColor(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        ColorPickerWindow dialog = new(viewModel.SelectedOutlineColor)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            viewModel.SelectedOutlineColor = dialog.SelectedColor;
        }
    }
}