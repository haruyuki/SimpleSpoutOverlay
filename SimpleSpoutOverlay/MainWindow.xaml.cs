using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SimpleSpoutOverlay.Models;
using SimpleSpoutOverlay.UI.Dialogs;
using SimpleSpoutOverlay.UI.ViewModels;

namespace SimpleSpoutOverlay;

/// Interaction logic for MainWindow.xaml
public partial class MainWindow
{
    private const double PreviewWidth = 1920;
    private const double PreviewHeight = 1080;
    private const double SnapThreshold = 12;

    private Point _dragStartPoint;
    private InsertionAdorner? _insertionAdorner;
    private ListBoxItem? _dropTargetItem;
    private bool _insertAfterTarget;
    private bool _isPreviewDragging;
    private LayerBase? _previewDraggedLayer;
    private Point _previewDragStartPoint;
    private double _previewLayerStartX;
    private double _previewLayerStartY;

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

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        Point point = e.GetPosition(PreviewSurface);
        LayerBase? hitLayer = HitTestLayer(point, viewModel);
        if (hitLayer == null)
        {
            return;
        }

        viewModel.SelectedLayer = hitLayer;
        _previewDraggedLayer = hitLayer;
        _previewDragStartPoint = point;
        _previewLayerStartX = hitLayer.PositionX;
        _previewLayerStartY = hitLayer.PositionY;
        _isPreviewDragging = true;
        ShowPreviewLayerOutline(hitLayer, isDragging: true);

        viewModel.BeginSliderUndoGesture();
        PreviewSurface.CaptureMouse();
        Mouse.OverrideCursor = Cursors.SizeAll;
        e.Handled = true;
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        Point point = e.GetPosition(PreviewSurface);

        if (_isPreviewDragging && _previewDraggedLayer != null)
        {
            Vector delta = point - _previewDragStartPoint;
            double targetX = _previewLayerStartX + delta.X;
            double targetY = _previewLayerStartY + delta.Y;

            if (viewModel.IsSnappingEnabled)
            {
                (targetX, targetY) = GetSnappedPosition(_previewDraggedLayer, targetX, targetY);
            }

            viewModel.SelectedPositionX = targetX;
            viewModel.SelectedPositionY = targetY;
            ShowPreviewLayerOutline(_previewDraggedLayer, isDragging: true);
            e.Handled = true;
            return;
        }

        LayerBase? hoverLayer = HitTestLayer(point, viewModel);
        PreviewSurface.Cursor = hoverLayer != null ? Cursors.SizeAll : Cursors.Arrow;
        ShowPreviewLayerOutline(hoverLayer, isDragging: false);
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPreviewDragging || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        EndPreviewDrag(viewModel);
        e.Handled = true;
    }

    private void EndPreviewDrag(MainWindowViewModel viewModel)
    {
        if (!_isPreviewDragging)
        {
            return;
        }

        _isPreviewDragging = false;
        _previewDraggedLayer = null;
        PreviewSurface.ReleaseMouseCapture();
        Mouse.OverrideCursor = null;
        viewModel.CommitSliderUndoGesture();

        Point pointer = Mouse.GetPosition(PreviewSurface);
        if (!IsPointInsidePreviewSurface(pointer))
        {
            HidePreviewLayerOutline();
            PreviewSurface.Cursor = Cursors.Arrow;
            return;
        }

        LayerBase? hoverLayer = HitTestLayer(pointer, viewModel);
        PreviewSurface.Cursor = hoverLayer != null ? Cursors.SizeAll : Cursors.Arrow;
        ShowPreviewLayerOutline(hoverLayer, isDragging: false);
    }

    private void OnPreviewMouseLeave(object sender, MouseEventArgs e)
    {
        if (_isPreviewDragging)
        {
            return;
        }

        PreviewSurface.Cursor = Cursors.Arrow;
        HidePreviewLayerOutline();
    }

    private LayerBase? HitTestLayer(Point point, MainWindowViewModel viewModel)
    {
        foreach (LayerBase layer in viewModel.Layers)
        {
            if (layer is TextLayer textLayer)
            {
                // Text uses a hybrid hit test so clicks in counters/glyph gaps still select the top text layer.
                if (LayerContainsPoint(textLayer, point) || TextBoundsContainsPoint(textLayer, point))
                {
                    return textLayer;
                }

                continue;
            }

            if (LayerContainsPoint(layer, point))
            {
                return layer;
            }
        }

        return null;
    }

    private static bool LayerContainsPoint(LayerBase layer, Point point)
    {
        switch (layer)
        {
            case ImageLayer imageLayer:
                return GetImageLayerBounds(imageLayer).Contains(point);
            case TextLayer textLayer:
            {
                Geometry? geometry = BuildTextLayerGeometry(textLayer);
                if (geometry == null)
                {
                    return false;
                }

                if (geometry.FillContains(point))
                {
                    return true;
                }

                if (textLayer is not { OutlineEnabled: true, OutlineThickness: > 0 }) return false;
                Pen outlinePen = new(Brushes.Transparent, textLayer.OutlineThickness)
                {
                    LineJoin = PenLineJoin.Round,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                };

                return geometry.StrokeContains(outlinePen, point);

            }
            default:
                return false;
        }
    }

    private static bool TextBoundsContainsPoint(TextLayer layer, Point point)
    {
        Rect bounds = GetTextLayerBounds(layer);
        return !bounds.IsEmpty && bounds.Contains(point);
    }

    private void ShowPreviewLayerOutline(LayerBase? layer, bool isDragging)
    {
        if (layer == null)
        {
            HidePreviewLayerOutline();
            return;
        }

        Geometry? geometry = GetLayerGeometry(layer);
        if (geometry == null)
        {
            HidePreviewLayerOutline();
            return;
        }

        PreviewLayerOutline.Data = geometry;
        PreviewLayerOutline.StrokeThickness = isDragging ? 3 : 2;
        PreviewLayerOutline.Fill = isDragging
            ? new SolidColorBrush(Color.FromArgb(48, 79, 157, 255))
            : new SolidColorBrush(Color.FromArgb(28, 79, 157, 255));
        PreviewLayerOutline.Visibility = Visibility.Visible;
    }

    private void HidePreviewLayerOutline()
    {
        PreviewLayerOutline.Data = null;
        PreviewLayerOutline.Visibility = Visibility.Collapsed;
    }

    private static bool IsPointInsidePreviewSurface(Point point)
    {
        return point is { X: >= 0 and <= PreviewWidth, Y: >= 0 and <= PreviewHeight };
    }

    private static (double X, double Y) GetSnappedPosition(LayerBase layer, double targetX, double targetY)
    {
        Rect bounds = GetLayerBoundsAtPosition(layer, targetX, targetY);
        if (bounds.IsEmpty)
        {
            return (targetX, targetY);
        }

        double snappedX = targetX + GetBestSnapOffset(bounds.Left, bounds.Left + (bounds.Width / 2.0), bounds.Right,
            0, PreviewWidth / 2.0, PreviewWidth);
        double snappedY = targetY + GetBestSnapOffset(bounds.Top, bounds.Top + (bounds.Height / 2.0), bounds.Bottom,
            0, PreviewHeight / 2.0, PreviewHeight);

        return (snappedX, snappedY);
    }

    private static Rect GetLayerBoundsAtPosition(LayerBase layer, double targetX, double targetY)
    {
        Rect bounds = layer switch
        {
            TextLayer textLayer => GetTextLayerBounds(textLayer),
            ImageLayer imageLayer => GetImageLayerBounds(imageLayer),
            _ => Rect.Empty
        };

        if (bounds.IsEmpty)
        {
            return Rect.Empty;
        }

        bounds.Offset(targetX - layer.PositionX, targetY - layer.PositionY);
        return bounds;
    }

    private static double GetBestSnapOffset(
        double movingStart,
        double movingCenter,
        double movingEnd,
        double targetStart,
        double targetCenter,
        double targetEnd)
    {
        double bestOffset = 0;
        double bestDistance = SnapThreshold + 1;

        TryCaptureBest(targetStart - movingStart);
        TryCaptureBest(targetCenter - movingCenter);
        TryCaptureBest(targetEnd - movingEnd);

        return bestOffset;

        void TryCaptureBest(double candidate)
        {
            double distance = Math.Abs(candidate);
            if (distance > SnapThreshold || distance >= bestDistance)
            {
                return;
            }

            bestOffset = candidate;
            bestDistance = distance;
        }
    }

    private static Geometry? GetLayerGeometry(LayerBase layer)
    {
        return layer switch
        {
            ImageLayer imageLayer => GetImageLayerBounds(imageLayer) is { IsEmpty: false } bounds
                ? new RectangleGeometry(bounds)
                : null,
            TextLayer textLayer => GetTextLayerBounds(textLayer) is { IsEmpty: false } bounds
                ? new RectangleGeometry(bounds)
                : null,
            _ => null
        };
    }

    private static Rect GetTextLayerBounds(TextLayer layer)
    {
        Geometry? geometry = BuildTextLayerGeometry(layer);
        if (geometry == null)
        {
            return Rect.Empty;
        }

        Rect bounds = geometry.Bounds;
        if (layer is { OutlineEnabled: true, OutlineThickness: > 0 })
        {
            bounds.Inflate(layer.OutlineThickness / 2.0, layer.OutlineThickness / 2.0);
        }

        return bounds;
    }

    private static Rect GetImageLayerBounds(ImageLayer layer)
    {
        if (!TryGetImagePixelSize(layer.ImagePath, out int pixelWidth, out int pixelHeight))
        {
            return Rect.Empty;
        }

        double drawWidth = pixelWidth * Math.Max(layer.ScaleX, 0.01);
        double drawHeight = pixelHeight * Math.Max(layer.ScaleY, 0.01);
        return new Rect(layer.PositionX, layer.PositionY, drawWidth, drawHeight);
    }

    private static Geometry? BuildTextLayerGeometry(TextLayer layer)
    {
        if (string.IsNullOrWhiteSpace(layer.Text))
        {
            return null;
        }

        Typeface typeface = new(new FontFamily(layer.FontFamily), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        FormattedText formattedText = new(
            layer.Text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            layer.FontSize,
            Brushes.Black,
            1.0);

        double clampedLineHeightMultiplier = Math.Max(layer.LineHeightMultiplier, 0.1);
        formattedText.LineHeight = layer.FontSize * clampedLineHeightMultiplier;

        double alignmentWidth = Math.Max(formattedText.WidthIncludingTrailingWhitespace, 1.0);
        formattedText.MaxTextWidth = alignmentWidth;
        formattedText.TextAlignment = layer.TextAlignment;

        Geometry geometry = formattedText.BuildGeometry(new Point(0, 0));
        TransformGroup transformGroup = new();
        transformGroup.Children.Add(new ScaleTransform(layer.ScaleX, layer.ScaleY));
        transformGroup.Children.Add(new TranslateTransform(layer.PositionX, layer.PositionY));
        geometry.Transform = transformGroup;

        return geometry;
    }

    private static bool TryGetImagePixelSize(string path, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            string normalizedPath = Path.GetFullPath(path);
            if (!File.Exists(normalizedPath))
            {
                return false;
            }

            BitmapFrame frame = BitmapFrame.Create(new Uri(normalizedPath, UriKind.Absolute), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            width = frame.PixelWidth;
            height = frame.PixelHeight;
            return width > 0 && height > 0;
        }
        catch
        {
            return false;
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