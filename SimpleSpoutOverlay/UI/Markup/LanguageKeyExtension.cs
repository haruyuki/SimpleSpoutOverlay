using System.Windows.Data;
using System.Windows.Markup;
using SimpleSpoutOverlay.Services;

namespace SimpleSpoutOverlay.UI.Markup;

/// Markup extension for easy access to localized strings in XAML.
/// Usage: {local:LanguageKey Label.Layers}
/// Shows key name in designer, uses localized text at runtime.
public class LanguageKeyExtension(string key) : MarkupExtension
{
    [ConstructorArgument("key")] private string Key { get; } = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        Binding binding = LocalizationService.CreateBinding(Key, Key);
        return binding.ProvideValue(serviceProvider);
    }
}