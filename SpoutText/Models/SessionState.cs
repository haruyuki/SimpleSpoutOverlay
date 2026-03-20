namespace SpoutText.Models;

/// <summary>
/// Serializable application state for restoring the previous editing session.
/// </summary>
public sealed class SessionState
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public List<TextLayerState> Layers { get; set; } = [];

    public int SelectedLayerIndex { get; init; } = -1;
}

/// <summary>
/// Serializable text layer data used in session files.
/// </summary>
public sealed class TextLayerState
{
    public string Text { get; set; } = string.Empty;

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
}

