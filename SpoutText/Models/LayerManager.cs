using System.Collections.ObjectModel;

namespace SpoutText.Models
{
    /// <summary>
    /// Manages the collection of text layers, including ordering and selection.
    /// </summary>
    public class LayerManager
    {
        private readonly ObservableCollection<TextLayer> _layers = [];

        public ObservableCollection<TextLayer> Layers => _layers;

        private TextLayer? SelectedLayer { get; set; }

        public event Action? LayersChanged;
        public event Action? SelectionChanged;

        public void AddLayer(TextLayer layer)
        {
            _layers.Add(layer);
            SelectedLayer = layer;
            LayersChanged?.Invoke();
        }

        public void RemoveLayer(TextLayer layer)
        {
            _layers.Remove(layer);
            if (SelectedLayer == layer)
            {
                SelectedLayer = _layers.Count > 0 ? _layers[^1] : null;
            }
            LayersChanged?.Invoke();
        }

        public void SelectLayer(TextLayer layer)
        {
            SelectedLayer = layer;
            SelectionChanged?.Invoke();
        }

        public void MoveLayerUp(TextLayer layer)
        {
            int index = _layers.IndexOf(layer);
            if (index <= 0) return;
            MoveLayer(index, index - 1);
        }

        public void MoveLayerDown(TextLayer layer)
        {
            int index = _layers.IndexOf(layer);
            if (index >= _layers.Count - 1) return;
            MoveLayer(index, index + 1);
        }

        public void MoveLayer(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _layers.Count) return;
            if (toIndex < 0 || toIndex >= _layers.Count) return;
            if (fromIndex == toIndex) return;

            _layers.Move(fromIndex, toIndex);
            LayersChanged?.Invoke();
        }
    }
}

