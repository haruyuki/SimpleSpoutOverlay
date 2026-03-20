using System.Windows;
using System.Windows.Media;
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
    }

    private void OnChooseFillColor(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        var dialog = new ColorPickerWindow(viewModel.SelectedFillColor)
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

        var dialog = new ColorPickerWindow(viewModel.SelectedOutlineColor)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            viewModel.SelectedOutlineColor = dialog.SelectedColor;
        }
    }
}