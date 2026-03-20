using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using SpoutText.Models;
using SpoutText.Rendering;

namespace SpoutText.UI.ViewModels
{
    public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly LayerManager _layerManager;
        private TextLayerRenderer? _renderer;
        private RenderTargetBitmap? _previewBitmap;
        private TextLayer? _selectedLayer;
        private bool _isRenderingPreview;
        private SpoutOutputManager? _spoutManager;
        private bool _spoutEnabled;
        private bool _disposed;

        // Selected layer properties
        private string _selectedText = "";
        private string _selectedFontFamily = "Arial";
        private double _selectedFontSize = 48;
        private Color _selectedFillColor = Colors.White;
        private double _selectedPositionX = 10;
        private double _selectedPositionY = 10;
        private double _selectedScaleX = 1.0;
        private double _selectedScaleY = 1.0;
        private bool _selectedOutlineEnabled;
        private Color _selectedOutlineColor = Colors.Black;
        private double _selectedOutlineThickness = 2;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Commands
        [UsedImplicitly]
        public RelayCommand AddLayerCommand {get; }
        [UsedImplicitly]
        public RelayCommand DeleteLayerCommand { get; }
        [UsedImplicitly]
        public RelayCommand MoveLayerUpCommand { get; }
        [UsedImplicitly]
        public RelayCommand MoveLayerDownCommand { get; }
        [UsedImplicitly]
        public RelayCommand ToggleSpoutCommand { get; }

        public MainWindowViewModel()
        {
            _layerManager = new LayerManager();
            _layerManager.LayersChanged += OnLayersChanged;
            _layerManager.SelectionChanged += OnSelectionChanged;

            AddLayerCommand = new RelayCommand(_ => AddLayer());
            DeleteLayerCommand = new RelayCommand(_ => DeleteLayer(), _ => SelectedLayer != null);
            MoveLayerUpCommand = new RelayCommand(_ => MoveLayerUp(), _ => SelectedLayer != null);
            MoveLayerDownCommand = new RelayCommand(_ => MoveLayerDown(), _ => SelectedLayer != null);
            ToggleSpoutCommand = new RelayCommand(_ => ToggleSpout());

            InitializeRenderer();
            InitializePreview();
            InitializeSpout();
        }

        private void InitializeRenderer()
        {
            _renderer = new TextLayerRenderer(1920, 1080);
        }

        private void InitializePreview()
        {
            _previewBitmap = new RenderTargetBitmap(1920, 1080, 96, 96, PixelFormats.Pbgra32);
            OnPropertyChanged(nameof(PreviewBitmap));
        }

        private void InitializeSpout()
        {
            try
            {
                _spoutManager = new SpoutOutputManager(1920, 1080);
                _spoutEnabled = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize Spout: {ex.Message}");
                _spoutManager = null;
            }
        }
    
        [UsedImplicitly]
        public ObservableCollection<TextLayer> Layers => _layerManager.Layers;
        
        [UsedImplicitly]
        public bool SpoutEnabled
        {
            get => _spoutEnabled;
            private set
            {
                if (_spoutEnabled == value) return;
                _spoutEnabled = value;
                OnPropertyChanged();
            }
        }
        
        [UsedImplicitly]
        public TextLayer? SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                if (_selectedLayer == value) return;
                _selectedLayer = value;
                if (value != null)
                {
                    _layerManager.SelectLayer(value);
                }
                UpdateSelectedLayerProperties();
                OnPropertyChanged();
            }
        }

        #region Selected Layer Properties

        public string SelectedText
        {
            get => _selectedText;
            set
            {
                if (_selectedText == value || _selectedLayer == null) return;
                _selectedText = value;
                _selectedLayer.Text = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public string SelectedFontFamily
        {
            get => _selectedFontFamily;
            set
            {
                if (_selectedFontFamily == value || _selectedLayer == null) return;
                _selectedFontFamily = value;
                _selectedLayer.FontFamily = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public double SelectedFontSize
        {
            get => _selectedFontSize;
            set
            {
                if (!(Math.Abs(_selectedFontSize - value) > 0.01) || _selectedLayer == null) return;
                _selectedFontSize = value;
                _selectedLayer.FontSize = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public Color SelectedFillColor
        {
            get => _selectedFillColor;
            set
            {
                if (_selectedFillColor == value || _selectedLayer == null) return;
                _selectedFillColor = value;
                _selectedLayer.FillColor = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public double SelectedPositionX
        {
            get => _selectedPositionX;
            set
            {
                if (!(Math.Abs(_selectedPositionX - value) > 0.01) || _selectedLayer == null) return;
                _selectedPositionX = value;
                _selectedLayer.PositionX = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public double SelectedPositionY
        {
            get => _selectedPositionY;
            set
            {
                if (!(Math.Abs(_selectedPositionY - value) > 0.01) || _selectedLayer == null) return;
                _selectedPositionY = value;
                _selectedLayer.PositionY = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public double SelectedScaleX
        {
            get => _selectedScaleX;
            set
            {
                if (!(Math.Abs(_selectedScaleX - value) > 0.01) || _selectedLayer == null) return;
                _selectedScaleX = value;
                _selectedLayer.ScaleX = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public double SelectedScaleY
        {
            get => _selectedScaleY;
            set
            {
                if (!(Math.Abs(_selectedScaleY - value) > 0.01) || _selectedLayer == null) return;
                _selectedScaleY = value;
                _selectedLayer.ScaleY = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public bool SelectedOutlineEnabled
        {
            get => _selectedOutlineEnabled;
            set
            {
                if (_selectedOutlineEnabled == value || _selectedLayer == null) return;
                _selectedOutlineEnabled = value;
                _selectedLayer.OutlineEnabled = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public Color SelectedOutlineColor
        {
            get => _selectedOutlineColor;
            set
            {
                if (_selectedOutlineColor == value || _selectedLayer == null) return;
                _selectedOutlineColor = value;
                _selectedLayer.OutlineColor = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public double SelectedOutlineThickness
        {
            get => _selectedOutlineThickness;
            set
            {
                if (!(Math.Abs(_selectedOutlineThickness - value) > 0.01) || _selectedLayer == null) return;
                _selectedOutlineThickness = value;
                _selectedLayer.OutlineThickness = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        #endregion

        public RenderTargetBitmap? PreviewBitmap
        {
            get => _previewBitmap;
            private set
            {
                if (_previewBitmap == value) return;
                _previewBitmap = value;
                OnPropertyChanged();
            }
        }
        
        [UsedImplicitly]
        public static IEnumerable<string> AvailableFonts
        {
            get
            {
                IOrderedEnumerable<string> fontFamilies = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(x => x);
                return fontFamilies;
            }
        }

        private void AddLayer()
        {
            TextLayer layer = new($"Text {_layerManager.Layers.Count + 1}");
            _layerManager.AddLayer(layer);
            SelectedLayer = layer;
        }

        private void DeleteLayer()
        {
            if (SelectedLayer == null) return;
            _layerManager.RemoveLayer(SelectedLayer);
            SelectedLayer = _layerManager.Layers.Count > 0 ? _layerManager.Layers[^1] : null;
        }

        private void MoveLayerUp()
        {
            if (SelectedLayer != null)
            {
                _layerManager.MoveLayerUp(SelectedLayer);
            }
        }

        private void MoveLayerDown()
        {
            if (SelectedLayer != null)
            {
                _layerManager.MoveLayerDown(SelectedLayer);
            }
        }

        private void UpdateSelectedLayerProperties()
        {
            if (_selectedLayer == null)
            {
                _selectedText = "";
                _selectedFontFamily = "Arial";
                _selectedFontSize = 48;
                _selectedFillColor = Colors.White;
                _selectedPositionX = 10;
                _selectedPositionY = 10;
                _selectedScaleX = 1.0;
                _selectedScaleY = 1.0;
                _selectedOutlineEnabled = false;
                _selectedOutlineColor = Colors.Black;
                _selectedOutlineThickness = 2;
            }
            else
            {
                _selectedText = _selectedLayer.Text;
                _selectedFontFamily = _selectedLayer.FontFamily;
                _selectedFontSize = _selectedLayer.FontSize;
                _selectedFillColor = _selectedLayer.FillColor;
                _selectedPositionX = _selectedLayer.PositionX;
                _selectedPositionY = _selectedLayer.PositionY;
                _selectedScaleX = _selectedLayer.ScaleX;
                _selectedScaleY = _selectedLayer.ScaleY;
                _selectedOutlineEnabled = _selectedLayer.OutlineEnabled;
                _selectedOutlineColor = _selectedLayer.OutlineColor;
                _selectedOutlineThickness = _selectedLayer.OutlineThickness;
            }

            OnPropertyChanged(nameof(SelectedText));
            OnPropertyChanged(nameof(SelectedFontFamily));
            OnPropertyChanged(nameof(SelectedFontSize));
            OnPropertyChanged(nameof(SelectedFillColor));
            OnPropertyChanged(nameof(SelectedPositionX));
            OnPropertyChanged(nameof(SelectedPositionY));
            OnPropertyChanged(nameof(SelectedScaleX));
            OnPropertyChanged(nameof(SelectedScaleY));
            OnPropertyChanged(nameof(SelectedOutlineEnabled));
            OnPropertyChanged(nameof(SelectedOutlineColor));
            OnPropertyChanged(nameof(SelectedOutlineThickness));
        }

        private void RefreshPreview()
        {
            if (_isRenderingPreview || _renderer == null)
                return;

            _isRenderingPreview = true;
            try
            {
                RenderTargetBitmap bitmap = _renderer.RenderLayers(_layerManager.Layers);
                PreviewBitmap = bitmap;

                // Send to Spout if enabled
                if (_spoutEnabled && _spoutManager != null)
                {
                    _spoutManager.SendFrame(bitmap);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error rendering preview: {ex.Message}");
            }
            finally
            {
                _isRenderingPreview = false;
            }
        }

        private void ToggleSpout()
        {
            if (_spoutManager == null)
            {
                return;
            }

            if (_spoutEnabled)
            {
                // Disable Spout
                _spoutManager.Shutdown();
                SpoutEnabled = false;
            }
            else
            {
                // Enable Spout
                if (_spoutManager.Initialize())
                {
                    SpoutEnabled = true;
                    // Send current frame
                    if (_previewBitmap != null)
                    {
                        _spoutManager.SendFrame(_previewBitmap);
                    }
                }
                else
                {
                    Debug.WriteLine("Failed to initialize Spout output");
                }
            }
        }

        private void OnLayersChanged()
        {
            RefreshPreview();
        }

        private void OnSelectionChanged()
        {
            // Preview updated via selection change in view
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_spoutEnabled && _spoutManager != null)
            {
                _spoutManager.Shutdown();
            }

            _spoutManager?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Simple relay command implementation for ICommand.
    /// </summary>
    public class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            execute(parameter);
        }
    }
}


