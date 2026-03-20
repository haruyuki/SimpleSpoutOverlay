using System.Collections.ObjectModel;

namespace SimpleSpoutOverlay.Models
{
    /// Manages the collection of layers, including ordering and selection.
    public class LayerManager
    {
        public ObservableCollection<LayerBase> Layers { get; } = [];

        private LayerBase? SelectedLayer { get; set; }

        public event Action? LayersChanged;
        public event Action? SelectionChanged;

        public void AddLayer(LayerBase layer)
        {
            Layers.Add(layer);
            SelectedLayer = layer;
            LayersChanged?.Invoke();
        }

        public void RemoveLayer(LayerBase layer)
        {
            int removedIndex = Layers.IndexOf(layer);
            Layers.Remove(layer);
            if (SelectedLayer == layer)
            {
                if (Layers.Count == 0)
                {
                    SelectedLayer = null;
                }
                else
                {
                    int nextIndex = Math.Min(Math.Max(removedIndex, 0), Layers.Count - 1);
                    SelectedLayer = Layers[nextIndex];
                }
            }
            LayersChanged?.Invoke();
        }

        public void SelectLayer(LayerBase layer)
        {
            SelectedLayer = layer;
            SelectionChanged?.Invoke();
        }

        public void MoveLayerUp(LayerBase layer)
        {
            int index = Layers.IndexOf(layer);
            if (index <= 0) return;
            MoveLayer(index, index - 1);
        }

        public void MoveLayerDown(LayerBase layer)
        {
            int index = Layers.IndexOf(layer);
            if (index >= Layers.Count - 1) return;
            MoveLayer(index, index + 1);
        }

        public void MoveLayer(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= Layers.Count) return;
            if (toIndex < 0 || toIndex >= Layers.Count) return;
            if (fromIndex == toIndex) return;

            Layers.Move(fromIndex, toIndex);
            LayersChanged?.Invoke();
        }
    }
}
