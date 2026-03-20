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
using SimpleSpoutOverlay.Models;
using SimpleSpoutOverlay.Rendering;
using SimpleSpoutOverlay.Services;

namespace SimpleSpoutOverlay.UI.ViewModels
{
    public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly LayerManager _layerManager;
        private TextLayerRenderer? _renderer;
        private RenderTargetBitmap? _previewBitmap;
        private LayerBase? _selectedLayer;
        private bool _isRenderingPreview;
        private SpoutOutputManager? _spoutManager;
        private bool _spoutEnabled;
        private bool _disposed;
        private readonly SessionPersistenceService _sessionPersistenceService;
        private readonly Stack<SessionState> _undoStack = new();
        private readonly Stack<SessionState> _redoStack = new();
        private bool _suppressHistory;
        private bool _isSliderUndoGestureActive;
        private SessionState? _sliderUndoSnapshot;
        private readonly DispatcherTimer _toastTimer;
        private string _toastMessage = string.Empty;
        private bool _isToastVisible;
        private bool _toastIsError;

        private const double MinFontSize = 8;
        private const double MaxFontSize = 200;
        private const double MinPositionX = 0;
        private const double MaxPositionX = 1920;
        private const double MinPositionY = 0;
        private const double MaxPositionY = 1080;
        private const double MinScale = 0.1;
        private const double MaxScale = 3;
        private const double MinOutlineThickness = 0.5;
        private const double MaxOutlineThickness = 20;

        private string _selectedText = string.Empty;
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
        private string _selectedImagePath = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Commands
        [UsedImplicitly]
        public RelayCommand AddLayerCommand { get; }
        [UsedImplicitly]
        public RelayCommand AddImageLayerCommand { get; }
        [UsedImplicitly]
        public RelayCommand ReplaceImageCommand { get; }
        [UsedImplicitly]
        public RelayCommand DeleteLayerCommand { get; }
        [UsedImplicitly]
        public RelayCommand MoveLayerUpCommand { get; }
        [UsedImplicitly]
        public RelayCommand MoveLayerDownCommand { get; }
        [UsedImplicitly]
        public RelayCommand ToggleSpoutCommand { get; }
        [UsedImplicitly]
        public RelayCommand UndoCommand { get; }
        [UsedImplicitly]
        public RelayCommand RedoCommand { get; }
        [UsedImplicitly]
        public RelayCommand SaveSetupCommand { get; }
        [UsedImplicitly]
        public RelayCommand LoadSetupCommand { get; }

        public MainWindowViewModel()
        {
            _layerManager = new LayerManager();
            _layerManager.LayersChanged += OnLayersChanged;
            _layerManager.SelectionChanged += OnSelectionChanged;

            AddLayerCommand = new RelayCommand(_ => AddTextLayer());
            AddImageLayerCommand = new RelayCommand(_ => ExecuteAddImageLayer());
            ReplaceImageCommand = new RelayCommand(_ => ExecuteReplaceSelectedImage(), _ => IsImageLayerSelected);
            DeleteLayerCommand = new RelayCommand(_ => DeleteLayer(), _ => SelectedLayer != null);
            MoveLayerUpCommand = new RelayCommand(_ => MoveLayerUp(), _ => CanMoveLayerUp());
            MoveLayerDownCommand = new RelayCommand(_ => MoveLayerDown(), _ => CanMoveLayerDown());
            ToggleSpoutCommand = new RelayCommand(_ => ToggleSpout());
            UndoCommand = new RelayCommand(_ => Undo(), _ => CanUndo);
            RedoCommand = new RelayCommand(_ => Redo(), _ => CanRedo);
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
        public ObservableCollection<LayerBase> Layers => _layerManager.Layers;

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
        public LayerBase? SelectedLayer
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
                OnPropertyChanged(nameof(IsTextLayerSelected));
                OnPropertyChanged(nameof(IsImageLayerSelected));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        [UsedImplicitly]
        public bool IsTextLayerSelected => _selectedLayer is TextLayer;

        [UsedImplicitly]
        public bool IsImageLayerSelected => _selectedLayer is ImageLayer;

        #region Selected Layer Properties

        public string SelectedText
        {
            get => _selectedText;
            set
            {
                if (_selectedText == value || _selectedLayer is not TextLayer textLayer) return;
                PushUndoSnapshot();
                _selectedText = value;
                textLayer.Text = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public string SelectedFontFamily
        {
            get => _selectedFontFamily;
            set
            {
                if (_selectedFontFamily == value || _selectedLayer is not TextLayer textLayer) return;
                PushUndoSnapshot();
                _selectedFontFamily = value;
                textLayer.FontFamily = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public double SelectedFontSize
        {
            get => _selectedFontSize;
            set
            {
                value = Math.Clamp(value, MinFontSize, MaxFontSize);
                if (!(Math.Abs(_selectedFontSize - value) > 0.01) || _selectedLayer is not TextLayer textLayer) return;
                RecordPropertyUndo();
                _selectedFontSize = value;
                textLayer.FontSize = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public Color SelectedFillColor
        {
            get => _selectedFillColor;
            set
            {
                if (_selectedFillColor == value || _selectedLayer is not TextLayer textLayer) return;
                PushUndoSnapshot();
                _selectedFillColor = value;
                textLayer.FillColor = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public string SelectedImagePath
        {
            get => _selectedImagePath;
            set
            {
                if (_selectedImagePath == value || _selectedLayer is not ImageLayer imageLayer) return;
                PushUndoSnapshot();
                _selectedImagePath = value;
                imageLayer.ImagePath = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public double SelectedPositionX
        {
            get => _selectedPositionX;
            set
            {
                value = Math.Clamp(value, MinPositionX, MaxPositionX);
                if (!(Math.Abs(_selectedPositionX - value) > 0.01) || _selectedLayer == null) return;
                RecordPropertyUndo();
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
                value = Math.Clamp(value, MinPositionY, MaxPositionY);
                if (!(Math.Abs(_selectedPositionY - value) > 0.01) || _selectedLayer == null) return;
                RecordPropertyUndo();
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
                value = Math.Clamp(value, MinScale, MaxScale);
                if (!(Math.Abs(_selectedScaleX - value) > 0.01) || _selectedLayer == null) return;
                RecordPropertyUndo();
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
                value = Math.Clamp(value, MinScale, MaxScale);
                if (!(Math.Abs(_selectedScaleY - value) > 0.01) || _selectedLayer == null) return;
                RecordPropertyUndo();
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
                if (_selectedOutlineEnabled == value || _selectedLayer is not TextLayer textLayer) return;
                PushUndoSnapshot();
                _selectedOutlineEnabled = value;
                textLayer.OutlineEnabled = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public Color SelectedOutlineColor
        {
            get => _selectedOutlineColor;
            set
            {
                if (_selectedOutlineColor == value || _selectedLayer is not TextLayer textLayer) return;
                PushUndoSnapshot();
                _selectedOutlineColor = value;
                textLayer.OutlineColor = value;
                OnPropertyChanged();
                RefreshPreview();
            }
        }

        public double SelectedOutlineThickness
        {
            get => _selectedOutlineThickness;
            set
            {
                value = Math.Clamp(value, MinOutlineThickness, MaxOutlineThickness);
                if (!(Math.Abs(_selectedOutlineThickness - value) > 0.01) || _selectedLayer is not TextLayer textLayer) return;
                RecordPropertyUndo();
                _selectedOutlineThickness = value;
                textLayer.OutlineThickness = value;
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
        public bool CanUndo => _undoStack.Count > 0;

        [UsedImplicitly]
        public bool CanRedo => _redoStack.Count > 0;

        [UsedImplicitly]
        public static IEnumerable<string> AvailableFonts
        {
            get
            {
                IOrderedEnumerable<string> fontFamilies = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(x => x);
                return fontFamilies;
            }
        }

        private void AddTextLayer()
        {
            PushUndoSnapshot();
            TextLayer layer = new($"Text {_layerManager.Layers.OfType<TextLayer>().Count() + 1}");
            _layerManager.AddLayer(layer);
            SelectedLayer = layer;
        }

        private void ExecuteAddImageLayer()
        {
            string? path = SelectImagePath();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            PushUndoSnapshot();
            ImageLayer layer = new(path);
            _layerManager.AddLayer(layer);
            SelectedLayer = layer;
            ShowToast($"Image added: {Path.GetFileName(path)}");
        }

        private void ExecuteReplaceSelectedImage()
        {
            if (_selectedLayer is not ImageLayer)
            {
                return;
            }

            string? path = SelectImagePath();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            SelectedImagePath = path;
            ShowToast($"Image updated: {Path.GetFileName(path)}");
        }

        private static string? SelectImagePath()
        {
            OpenFileDialog dialog = new()
            {
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.webp|All Files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        private void DeleteLayer()
        {
            if (SelectedLayer == null) return;

            PushUndoSnapshot();
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
            if (SelectedLayer == null) return;
            PushUndoSnapshot();
            _layerManager.MoveLayerUp(SelectedLayer);
        }

        private void MoveLayerDown()
        {
            if (SelectedLayer == null) return;
            PushUndoSnapshot();
            _layerManager.MoveLayerDown(SelectedLayer);
        }

        public void ReorderLayer(LayerBase draggedLayer, LayerBase? targetLayer, bool insertAfter)
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

            PushUndoSnapshot();
            _layerManager.MoveLayer(fromIndex, toIndex);
            SelectedLayer = draggedLayer;
        }

        private void UpdateSelectedLayerProperties()
        {
            if (_selectedLayer == null)
            {
                _selectedText = string.Empty;
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
                _selectedImagePath = string.Empty;
            }
            else
            {
                _selectedPositionX = _selectedLayer.PositionX;
                _selectedPositionY = _selectedLayer.PositionY;
                _selectedScaleX = _selectedLayer.ScaleX;
                _selectedScaleY = _selectedLayer.ScaleY;

                if (_selectedLayer is TextLayer textLayer)
                {
                    _selectedText = textLayer.Text;
                    _selectedFontFamily = textLayer.FontFamily;
                    _selectedFontSize = textLayer.FontSize;
                    _selectedFillColor = textLayer.FillColor;
                    _selectedOutlineEnabled = textLayer.OutlineEnabled;
                    _selectedOutlineColor = textLayer.OutlineColor;
                    _selectedOutlineThickness = textLayer.OutlineThickness;
                    _selectedImagePath = string.Empty;
                }
                else if (_selectedLayer is ImageLayer imageLayer)
                {
                    _selectedText = string.Empty;
                    _selectedFontFamily = "Arial";
                    _selectedFontSize = 48;
                    _selectedFillColor = Colors.White;
                    _selectedOutlineEnabled = false;
                    _selectedOutlineColor = Colors.Black;
                    _selectedOutlineThickness = 2;
                    _selectedImagePath = imageLayer.ImagePath;
                }
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
            OnPropertyChanged(nameof(SelectedImagePath));
            OnPropertyChanged(nameof(IsTextLayerSelected));
            OnPropertyChanged(nameof(IsImageLayerSelected));
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

        private void PushUndoSnapshot()
        {
            if (_suppressHistory)
            {
                return;
            }

            _undoStack.Push(CloneSessionState(CaptureSessionState()));
            _redoStack.Clear();
            NotifyHistoryStateChanged();
        }

        private void RecordPropertyUndo()
        {
            if (_isSliderUndoGestureActive)
            {
                return;
            }

            PushUndoSnapshot();
        }

        public void BeginSliderUndoGesture()
        {
            if (_suppressHistory || _isSliderUndoGestureActive)
            {
                return;
            }

            _sliderUndoSnapshot = CloneSessionState(CaptureSessionState());
            _isSliderUndoGestureActive = true;
        }

        public void CommitSliderUndoGesture()
        {
            if (!_isSliderUndoGestureActive)
            {
                return;
            }

            SessionState? initialSnapshot = _sliderUndoSnapshot;
            _sliderUndoSnapshot = null;
            _isSliderUndoGestureActive = false;

            if (initialSnapshot == null)
            {
                return;
            }

            SessionState currentSnapshot = CaptureSessionState();
            if (AreSessionStatesEquivalent(initialSnapshot, currentSnapshot))
            {
                return;
            }

            _undoStack.Push(initialSnapshot);
            _redoStack.Clear();
            NotifyHistoryStateChanged();
        }

        public void CancelSliderUndoGesture()
        {
            _sliderUndoSnapshot = null;
            _isSliderUndoGestureActive = false;
        }

        private void Undo()
        {
            if (!CanUndo)
            {
                return;
            }

            _redoStack.Push(CloneSessionState(CaptureSessionState()));
            SessionState previous = _undoStack.Pop();
            RestoreSessionState(previous, clearHistory: false);
            NotifyHistoryStateChanged();
        }

        private void Redo()
        {
            if (!CanRedo)
            {
                return;
            }

            _undoStack.Push(CloneSessionState(CaptureSessionState()));
            SessionState next = _redoStack.Pop();
            RestoreSessionState(next, clearHistory: false);
            NotifyHistoryStateChanged();
        }

        private void NotifyHistoryStateChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            CommandManager.InvalidateRequerySuggested();
        }

        private static SessionState CloneSessionState(SessionState source)
        {
            return new SessionState
            {
                Version = source.Version,
                SelectedLayerIndex = source.SelectedLayerIndex,
                Layers = source.Layers
                    .Select(layer => new LayerState
                    {
                        Type = layer.Type,
                        Text = layer.Text,
                        FontFamily = layer.FontFamily,
                        FontSize = layer.FontSize,
                        FillColor = layer.FillColor,
                        PositionX = layer.PositionX,
                        PositionY = layer.PositionY,
                        ScaleX = layer.ScaleX,
                        ScaleY = layer.ScaleY,
                        OutlineEnabled = layer.OutlineEnabled,
                        OutlineColor = layer.OutlineColor,
                        OutlineThickness = layer.OutlineThickness,
                        ImagePath = layer.ImagePath
                    })
                    .ToList()
            };
        }

        private static bool AreSessionStatesEquivalent(SessionState left, SessionState right)
        {
            if (left.SelectedLayerIndex != right.SelectedLayerIndex || left.Layers.Count != right.Layers.Count)
            {
                return false;
            }

            for (int index = 0; index < left.Layers.Count; index++)
            {
                LayerState a = left.Layers[index];
                LayerState b = right.Layers[index];

                if (a.Type != b.Type ||
                    a.Text != b.Text ||
                    a.FontFamily != b.FontFamily ||
                    Math.Abs(a.FontSize - b.FontSize) > 0.001 ||
                    a.FillColor != b.FillColor ||
                    Math.Abs(a.PositionX - b.PositionX) > 0.001 ||
                    Math.Abs(a.PositionY - b.PositionY) > 0.001 ||
                    Math.Abs(a.ScaleX - b.ScaleX) > 0.001 ||
                    Math.Abs(a.ScaleY - b.ScaleY) > 0.001 ||
                    a.OutlineEnabled != b.OutlineEnabled ||
                    a.OutlineColor != b.OutlineColor ||
                    Math.Abs(a.OutlineThickness - b.OutlineThickness) > 0.001 ||
                    a.ImagePath != b.ImagePath)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanMoveLayerUp()
        {
            if (SelectedLayer == null)
            {
                return false;
            }

            return _layerManager.Layers.IndexOf(SelectedLayer) > 0;
        }

        private bool CanMoveLayerDown()
        {
            if (SelectedLayer == null)
            {
                return false;
            }

            int index = _layerManager.Layers.IndexOf(SelectedLayer);
            return index >= 0 && index < _layerManager.Layers.Count - 1;
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
                Filter = "SimpleSpoutOverlay Session (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = "simplespoutoverlay-session.json",
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
                Filter = "SimpleSpoutOverlay Session (*.json)|*.json|All Files (*.*)|*.*",
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
                    PushUndoSnapshot();
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
                    RestoreSessionState(state, clearHistory: true);
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
                Layers = _layerManager.Layers.Select(ToLayerState).ToList()
            };
        }

        private void RestoreSessionState(SessionState state, bool clearHistory = false)
        {
            bool previousSuppressHistory = _suppressHistory;
            _suppressHistory = true;
            try
            {
                _layerManager.Layers.Clear();

                foreach (LayerState layerState in state.Layers)
                {
                    _layerManager.Layers.Add(FromLayerState(layerState));
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
            }
            finally
            {
                _suppressHistory = previousSuppressHistory;
            }

            if (clearHistory)
            {
                _undoStack.Clear();
                _redoStack.Clear();
            }

            NotifyHistoryStateChanged();

            RefreshPreview();
        }

        private static LayerState ToLayerState(LayerBase layer)
        {
            return layer switch
            {
                TextLayer textLayer => new LayerState
                {
                    Type = "Text",
                    Text = textLayer.Text,
                    FontFamily = textLayer.FontFamily,
                    FontSize = textLayer.FontSize,
                    FillColor = ToArgbHex(textLayer.FillColor),
                    PositionX = textLayer.PositionX,
                    PositionY = textLayer.PositionY,
                    ScaleX = textLayer.ScaleX,
                    ScaleY = textLayer.ScaleY,
                    OutlineEnabled = textLayer.OutlineEnabled,
                    OutlineColor = ToArgbHex(textLayer.OutlineColor),
                    OutlineThickness = textLayer.OutlineThickness
                },
                ImageLayer imageLayer => new LayerState
                {
                    Type = "Image",
                    PositionX = imageLayer.PositionX,
                    PositionY = imageLayer.PositionY,
                    ScaleX = imageLayer.ScaleX,
                    ScaleY = imageLayer.ScaleY,
                    ImagePath = imageLayer.ImagePath
                },
                _ => throw new InvalidOperationException("Unsupported layer type.")
            };
        }

        private static LayerBase FromLayerState(LayerState state)
        {
            if (string.Equals(state.Type, "Image", StringComparison.OrdinalIgnoreCase))
            {
                return new ImageLayer(state.ImagePath)
                {
                    PositionX = state.PositionX,
                    PositionY = state.PositionY,
                    ScaleX = state.ScaleX,
                    ScaleY = state.ScaleY
                };
            }

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

