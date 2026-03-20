using System.Collections.ObjectModel;

namespace SpoutText.Models
{
    /// <summary>
    /// Manages the collection of text layers, including ordering and selection.
    /// </summary>
    public class LayerManager
    {
        private ObservableCollection<TextLayer> _layers = new();
        private TextLayer? _selectedLayer;

        public ObservableCollection<TextLayer> Layers => _layers;

        public TextLayer? SelectedLayer
        {
            get => _selectedLayer;
            set => _selectedLayer = value;
        }

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
                SelectedLayer = _layers.Count > 0 ? _layers[_layers.Count - 1] : null;
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
            if (index > 0)
            {
                _layers.Move(index, index - 1);
                LayersChanged?.Invoke();
            }
        }

        public void MoveLayerDown(TextLayer layer)
        {
            int index = _layers.IndexOf(layer);
            if (index < _layers.Count - 1)
            {
                _layers.Move(index, index + 1);
                LayersChanged?.Invoke();
            }
        }

        public int GetLayerIndex(TextLayer layer)
        {
            return _layers.IndexOf(layer);
        }

        public void Clear()
        {
            _layers.Clear();
            SelectedLayer = null;
            LayersChanged?.Invoke();
        }
    }
}

