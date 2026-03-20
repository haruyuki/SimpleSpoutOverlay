using System.IO;

namespace SpoutText.Models;

public sealed class ImageLayer(string imagePath) : LayerBase
{
    public string ImagePath { get; set; } = imagePath;

    public override string LayerType => "Image";

    public override string DisplayName => string.IsNullOrWhiteSpace(ImagePath)
        ? "Image"
        : $"Image: {Path.GetFileName(ImagePath)}";
}
