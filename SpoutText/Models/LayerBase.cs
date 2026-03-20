using JetBrains.Annotations;

namespace SpoutText.Models;

/// Base layer type used by the renderer and layer list.
public abstract class LayerBase
{
    public double PositionX { get; set; } = 10;

    public double PositionY { get; set; } = 10;

    public double ScaleX { get; set; } = 1.0;

    public double ScaleY { get; set; } = 1.0;

    protected virtual string LayerType => "Layer";

    [UsedImplicitly]
    public virtual string DisplayName => LayerType;
}
