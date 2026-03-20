using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using JetBrains.Annotations;
using Microsoft.Win32;
using SpoutText.Models;
using SpoutText.Rendering;
using SpoutText.Services;

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
        private readonly SessionPersistenceService _sessionPersistenceService;
        private readonly DispatcherTimer _toastTimer;
        private string _toastMessage = string.Empty;
        private bool _isToastVisible;
        private bool _toastIsError;

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
        [UsedImplicitly]
        public RelayCommand SaveSetupCommand { get; }
        [UsedImplicitly]
        public RelayCommand LoadSetupCommand { get; }

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
            SaveSetupCommand = new RelayCommand(_ => ExecuteSaveSetup());
            LoadSetupCommand = new RelayCommand(_ => ExecuteLoadSetup());
            _sessionPersistenceService = new SessionPersistenceService();
            _toastTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _toastTimer.Tick += OnToastTimerTick;

            InitializeRenderer();
            InitializePreview();
            InitializeSpout();
            TryLoadDefaultSession();
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
        public string ToastMessage
        {
            get => _toastMessage;
            private set
            {
                if (_toastMessage == value) return;
                _toastMessage = value;
                OnPropertyChanged();
            }
        }

        [UsedImplicitly]
        public bool IsToastVisible
        {
            get => _isToastVisible;
            private set
            {
                if (_isToastVisible == value) return;
                _isToastVisible = value;
                OnPropertyChanged();
            }
        }

        [UsedImplicitly]
        public bool ToastIsError
        {
            get => _toastIsError;
            private set
            {
                if (_toastIsError == value) return;
                _toastIsError = value;
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
            int removedIndex = _layerManager.Layers.IndexOf(SelectedLayer);
            _layerManager.RemoveLayer(SelectedLayer);

            if (_layerManager.Layers.Count == 0)
            {
                SelectedLayer = null;
                return;
            }

            int nextIndex = Math.Min(Math.Max(removedIndex, 0), _layerManager.Layers.Count - 1);
            SelectedLayer = _layerManager.Layers[nextIndex];
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

        public void ReorderLayer(TextLayer draggedLayer, TextLayer? targetLayer, bool insertAfter)
        {
            int fromIndex = _layerManager.Layers.IndexOf(draggedLayer);
            if (fromIndex < 0)
            {
                return;
            }

            int toIndex;
            if (targetLayer == null)
            {
                toIndex = _layerManager.Layers.Count - 1;
            }
            else
            {
                int targetIndex = _layerManager.Layers.IndexOf(targetLayer);
                if (targetIndex < 0)
                {
                    return;
                }

                int rawInsertIndex = targetIndex + (insertAfter ? 1 : 0);
                if (fromIndex < rawInsertIndex)
                {
                    rawInsertIndex--;
                }

                toIndex = Math.Clamp(rawInsertIndex, 0, _layerManager.Layers.Count - 1);
            }

            if (toIndex < 0)
            {
                return;
            }

            _layerManager.MoveLayer(fromIndex, toIndex);
            SelectedLayer = draggedLayer;
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

        private void ExecuteSaveSetup()
        {
            SaveFileDialog dialog = new()
            {
                Filter = "SpoutText Session (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = "spouttext-session.json",
                InitialDirectory = Path.GetDirectoryName(_sessionPersistenceService.DefaultSessionPath)
            };

            if (dialog.ShowDialog() != true) return;
            try
            {
                SessionPersistenceService.SaveToPath(dialog.FileName, CaptureSessionState());
                ShowToast($"Setup saved: {Path.GetFileName(dialog.FileName)}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save session file: {ex.Message}");
                ShowToast("Failed to save setup file.", isError: true);
            }
        }

        private void ExecuteLoadSetup()
        {
            OpenFileDialog dialog = new()
            {
                Filter = "SpoutText Session (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".json",
                CheckFileExists = true,
                InitialDirectory = Path.GetDirectoryName(_sessionPersistenceService.DefaultSessionPath)
            };

            if (dialog.ShowDialog() != true) return;
            try
            {
                SessionState? state = SessionPersistenceService.LoadFromPath(dialog.FileName);
                if (state != null)
                {
                    RestoreSessionState(state);
                    ShowToast($"Setup loaded: {Path.GetFileName(dialog.FileName)}");
                }
                else
                {
                    ShowToast("Invalid or empty setup file.", isError: true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load session file: {ex.Message}");
                ShowToast("Failed to load setup file.", isError: true);
            }
        }

        private void TryLoadDefaultSession()
        {
            try
            {
                SessionState? state = SessionPersistenceService.LoadFromPath(_sessionPersistenceService.DefaultSessionPath);
                if (state != null)
                {
                    RestoreSessionState(state);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load default session: {ex.Message}");
            }
        }

        private void ShowToast(string message, bool isError = false)
        {
            ToastMessage = message;
            ToastIsError = isError;
            IsToastVisible = true;

            _toastTimer.Stop();
            _toastTimer.Start();
        }

        private void OnToastTimerTick(object? sender, EventArgs e)
        {
            _toastTimer.Stop();
            IsToastVisible = false;
        }

        public void SaveDefaultSession()
        {
            try
            {
                SessionPersistenceService.SaveToPath(_sessionPersistenceService.DefaultSessionPath, CaptureSessionState());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save default session: {ex.Message}");
            }
        }

        private SessionState CaptureSessionState()
        {
            int selectedIndex = SelectedLayer == null ? -1 : _layerManager.Layers.IndexOf(SelectedLayer);

            return new SessionState
            {
                Version = SessionState.CurrentVersion,
                SelectedLayerIndex = selectedIndex,
                Layers = _layerManager.Layers.Select(ToTextLayerState).ToList()
            };
        }

        private void RestoreSessionState(SessionState state)
        {
            _layerManager.Layers.Clear();

            foreach (TextLayerState layerState in state.Layers)
            {
                _layerManager.Layers.Add(FromTextLayerState(layerState));
            }

            if (_layerManager.Layers.Count == 0)
            {
                SelectedLayer = null;
            }
            else if (state.SelectedLayerIndex >= 0 && state.SelectedLayerIndex < _layerManager.Layers.Count)
            {
                SelectedLayer = _layerManager.Layers[state.SelectedLayerIndex];
            }
            else
            {
                SelectedLayer = _layerManager.Layers[0];
            }

            RefreshPreview();
        }

        private static TextLayerState ToTextLayerState(TextLayer layer)
        {
            return new TextLayerState
            {
                Text = layer.Text,
                FontFamily = layer.FontFamily,
                FontSize = layer.FontSize,
                FillColor = ToArgbHex(layer.FillColor),
                PositionX = layer.PositionX,
                PositionY = layer.PositionY,
                ScaleX = layer.ScaleX,
                ScaleY = layer.ScaleY,
                OutlineEnabled = layer.OutlineEnabled,
                OutlineColor = ToArgbHex(layer.OutlineColor),
                OutlineThickness = layer.OutlineThickness
            };
        }

        private static TextLayer FromTextLayerState(TextLayerState state)
        {
            return new TextLayer(state.Text, state.FontFamily, state.FontSize)
            {
                FillColor = ParseColor(state.FillColor, Colors.White),
                PositionX = state.PositionX,
                PositionY = state.PositionY,
                ScaleX = state.ScaleX,
                ScaleY = state.ScaleY,
                OutlineEnabled = state.OutlineEnabled,
                OutlineColor = ParseColor(state.OutlineColor, Colors.Black),
                OutlineThickness = state.OutlineThickness
            };
        }

        private static Color ParseColor(string value, Color fallback)
        {
            try
            {
                object? converted = ColorConverter.ConvertFromString(value);
                return converted is Color color ? color : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static string ToArgbHex(Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
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

            SaveDefaultSession();

            if (_spoutEnabled && _spoutManager != null)
            {
                _spoutManager.Shutdown();
            }

            _spoutManager?.Dispose();
            _toastTimer.Stop();
            _toastTimer.Tick -= OnToastTimerTick;
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


