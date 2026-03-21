namespace SimpleSpoutOverlay.Models;

/// Serializable application state for restoring the previous editing session.
public sealed class SessionState
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public List<LayerState> Layers { get; init; } = [];

    public int SelectedLayerIndex { get; init; } = -1;

    public bool IsSnappingEnabled { get; init; } = true;
}

/// Serializable layer data used in session files.
public sealed class LayerState
{
    public string Type { get; init; } = "Text";

    public string Text { get; init; } = string.Empty;

    public string FontFamily { get; init; } = "Arial";

    public double FontSize { get; init; } = 48;

    public string FillColor { get; init; } = "#FFFFFFFF";

    public double PositionX { get; init; } = 10;

    public double PositionY { get; init; } = 10;

    public double ScaleX { get; init; } = 1.0;

    public double ScaleY { get; init; } = 1.0;

    public bool OutlineEnabled { get; init; }

    public string OutlineColor { get; init; } = "#FF000000";

    public double OutlineThickness { get; init; } = 2;

    public string ImagePath { get; init; } = string.Empty;
}
