using System.Windows.Markup;
using System.Windows.Data;
using SimpleSpoutOverlay.Services;

namespace SimpleSpoutOverlay.UI.Markup
{
    /// <summary>
    /// Markup extension for easy access to localized strings in XAML.
    /// Usage: {local:LanguageKey Label.Layers}
    /// </summary>
    public class LanguageKeyExtension : MarkupExtension
    {
        private string _key = string.Empty;

        public LanguageKeyExtension()
        {
        }

        public LanguageKeyExtension(string key)
        {
            _key = key;
        }

        [ConstructorArgument("key")]
        public string Key
        {
            get => _key;
            set => _key = value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            Binding binding = new($"[{_key}]")
            {
                Source = LocalizationService.Instance,
                Mode = BindingMode.OneWay
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}


