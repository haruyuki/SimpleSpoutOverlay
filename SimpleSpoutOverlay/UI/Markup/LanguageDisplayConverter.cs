using System.Globalization;
using System.Windows.Data;
using SimpleSpoutOverlay.Services;

namespace SimpleSpoutOverlay.UI.Markup
{
    /// <summary>
    /// Converts language codes to display names.
    /// Usage: {Binding ., Converter={x:Static markup:LanguageDisplayConverter.Instance}}
    /// </summary>
    public sealed class LanguageDisplayConverter : IValueConverter
    {
        public static readonly LanguageDisplayConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string languageCode)
            {
                return value ?? string.Empty;
            }

            return LocalizationService.LanguageNames.TryGetValue(languageCode, out var displayName)
                ? displayName
                : languageCode;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

