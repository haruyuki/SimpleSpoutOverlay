using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using SimpleSpoutOverlay.Models;
using SimpleSpoutOverlay.UI.Dialogs;
using SimpleSpoutOverlay.UI.ViewModels;

namespace SimpleSpoutOverlay;

/// Interaction logic for MainWindow.xaml
public partial class MainWindow : Window
{
    private Point _dragStartPoint;
    private InsertionAdorner? _insertionAdorner;
    private ListBoxItem? _dropTargetItem;
    private bool _insertAfterTarget;

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

    private void OnLayerListPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void OnLayerListPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || sender is not ListBox listBox)
        {
            return;
        }

        Point mousePosition = e.GetPosition(null);
        Vector dragVector = _dragStartPoint - mousePosition;
        bool shouldStartDrag = Math.Abs(dragVector.X) >= SystemParameters.MinimumHorizontalDragDistance
                               || Math.Abs(dragVector.Y) >= SystemParameters.MinimumVerticalDragDistance;

        if (!shouldStartDrag)
        {
            return;
        }

        ListBoxItem? listBoxItem = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (listBoxItem?.DataContext is not LayerBase draggedLayer)
        {
            return;
        }

        DataObject dragData = new();
        dragData.SetData(typeof(LayerBase), draggedLayer);
        switch (draggedLayer)
        {
            case TextLayer textLayer:
                dragData.SetData(typeof(TextLayer), textLayer);
                break;
            case ImageLayer imageLayer:
                dragData.SetData(typeof(ImageLayer), imageLayer);
                break;
        }

        DragDrop.DoDragDrop(listBox, dragData, DragDropEffects.Move);
    }

    private void OnLayerListPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || Keyboard.Modifiers != ModifierKeys.Control)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Up when viewModel.MoveLayerUpCommand.CanExecute(null):
                viewModel.MoveLayerUpCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Down when viewModel.MoveLayerDownCommand.CanExecute(null):
                viewModel.MoveLayerDownCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }


    private void OnLayerListDragOver(object sender, DragEventArgs e)
    {
        if (sender is not ListBox listBox || !TryGetDraggedLayer(e.Data, out _))
        {
            ClearInsertionAdorner();
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        Point listPosition = e.GetPosition(listBox);
        if (TryResolveDropTarget(listBox, listPosition, out ListBoxItem? targetItem, out bool insertAfter))
        {
            ShowInsertionAdorner(targetItem, insertAfter);
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            ClearInsertionAdorner();
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void OnLayerListDragLeave(object sender, DragEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            Point position = e.GetPosition(listBox);
            bool isStillInside = position.X >= 0 && position.X <= listBox.ActualWidth
                                 && position.Y >= 0 && position.Y <= listBox.ActualHeight;
            if (isStillInside)
            {
                return;
            }
        }

        ClearInsertionAdorner();
    }

    private void OnLayerListDrop(object sender, DragEventArgs e)
    {
        if (sender is not ListBox || DataContext is not MainWindowViewModel viewModel || !TryGetDraggedLayer(e.Data, out LayerBase draggedLayer))
        {
            ClearInsertionAdorner();
            return;
        }

        if (sender is ListBox listBox)
        {
            Point listPosition = e.GetPosition(listBox);
            if (TryResolveDropTarget(listBox, listPosition, out ListBoxItem? targetItem, out bool insertAfter))
            {
                _dropTargetItem = targetItem;
                _insertAfterTarget = insertAfter;
            }
        }

        LayerBase? targetLayer = _dropTargetItem?.DataContext as LayerBase;
        viewModel.ReorderLayer(draggedLayer, targetLayer, _insertAfterTarget);
        ClearInsertionAdorner();
    }

    private static bool TryGetDraggedLayer(IDataObject data, out LayerBase layer)
    {
        if (data.GetData(typeof(LayerBase)) is LayerBase baseLayer)
        {
            layer = baseLayer;
            return true;
        }

        if (data.GetData(typeof(TextLayer)) is TextLayer textLayer)
        {
            layer = textLayer;
            return true;
        }

        if (data.GetData(typeof(ImageLayer)) is ImageLayer imageLayer)
        {
            layer = imageLayer;
            return true;
        }

        layer = null!;
        return false;
    }

    private static bool TryResolveDropTarget(ListBox listBox, Point listPosition, out ListBoxItem? targetItem, out bool insertAfter)
    {
        targetItem = null;
        insertAfter = false;

        if (listBox.Items.Count == 0)
        {
            return false;
        }

        for (int index = 0; index < listBox.Items.Count; index++)
        {
            if (listBox.ItemContainerGenerator.ContainerFromIndex(index) is not ListBoxItem item)
            {
                continue;
            }

            Rect itemBounds = GetItemBoundsInList(listBox, item);

            if (listPosition.Y < itemBounds.Top)
            {
                targetItem = item;
                insertAfter = false;
                return true;
            }

            if (!(listPosition.Y <= itemBounds.Bottom)) continue;
            targetItem = item;
            insertAfter = listPosition.Y >= itemBounds.Top + (itemBounds.Height / 2);
            return true;
        }

        if (listBox.ItemContainerGenerator.ContainerFromIndex(listBox.Items.Count - 1) is not ListBoxItem lastItem)
            return false;
        targetItem = lastItem;
        insertAfter = true;
        return true;

    }

    private static Rect GetItemBoundsInList(ListBox listBox, ListBoxItem item)
    {
        GeneralTransform transform = item.TransformToAncestor(listBox);
        return transform.TransformBounds(new Rect(new Point(0, 0), item.RenderSize));
    }

    private void ShowInsertionAdorner(ListBoxItem? targetItem, bool insertAfter)
    {
        if (targetItem == null)
        {
            ClearInsertionAdorner();
            return;
        }

        if (_dropTargetItem == targetItem && _insertAfterTarget == insertAfter && _insertionAdorner != null)
        {
            return;
        }

        ClearInsertionAdorner();

        AdornerLayer? adornerLayer = AdornerLayer.GetAdornerLayer(targetItem);
        if (adornerLayer == null)
        {
            return;
        }

        _dropTargetItem = targetItem;
        _insertAfterTarget = insertAfter;
        _insertionAdorner = new InsertionAdorner(targetItem, insertAfter);
        adornerLayer.Add(_insertionAdorner);
    }

    private void ClearInsertionAdorner()
    {
        if (_dropTargetItem != null && _insertionAdorner != null)
        {
            AdornerLayer? adornerLayer = AdornerLayer.GetAdornerLayer(_dropTargetItem);
            adornerLayer?.Remove(_insertionAdorner);
        }

        _insertionAdorner = null;
        _dropTargetItem = null;
        _insertAfterTarget = false;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T typed)
            {
                return typed;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private sealed class InsertionAdorner(UIElement adornedElement, bool insertAfter) : Adorner(adornedElement)
    {
        private static readonly Pen IndicatorPen = new(new SolidColorBrush(Color.FromRgb(79, 157, 255)), 2);

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double y = insertAfter ? AdornedElement.RenderSize.Height - 1 : 1;
            const double left = 4;
            double right = Math.Max(left + 1, AdornedElement.RenderSize.Width - 4);

            drawingContext.DrawLine(IndicatorPen, new Point(left, y), new Point(right, y));
            drawingContext.DrawEllipse(IndicatorPen.Brush, null, new Point(left, y), 2.5, 2.5);
            drawingContext.DrawEllipse(IndicatorPen.Brush, null, new Point(right, y), 2.5, 2.5);
        }
    }
}