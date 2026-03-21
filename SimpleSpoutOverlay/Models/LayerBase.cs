using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace SimpleSpoutOverlay.Models;

/// Base layer type used by the renderer and layer list.
public abstract class LayerBase : INotifyPropertyChanged
{
    public double PositionX { get; set; } = 10;

    public double PositionY { get; set; } = 10;

    public double ScaleX { get; set; } = 1.0;

    public double ScaleY { get; set; } = 1.0;

    protected virtual string LayerType => "Layer";
    
    public virtual string DisplayName => LayerType;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
