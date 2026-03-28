using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

namespace SimpleSpoutOverlay.Services;

/// Manages application localization and language switching.
/// Supports runtime language changes without app restart.
public sealed class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    private string _currentLanguageCode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public static LocalizationService Instance => _instance ??= new LocalizationService();

    // Supported languages
    private static readonly string[] SupportedLanguages = ["en-US", "zh-TW"];
    public static readonly Dictionary<string, string> LanguageNames = new()
    {
        { "en-US", "English" },
        { "zh-TW", "繁體中文" }
    };

    private LocalizationService()
    {
        _currentLanguageCode = "en-US";
    }
    
    /// Gets the current language code (e.g., "en-US", "zh-TW").
    public string CurrentLanguageCode
    {
        get => _currentLanguageCode;
        private set
        {
            if (_currentLanguageCode == value) return;
            _currentLanguageCode = value;
            OnPropertyChanged();
        }
    }
    
    /// Gets the list of available language codes.
    public static IReadOnlyList<string> AvailableLanguages => SupportedLanguages;
    
    /// Switches to the specified language and updates all UI bindings.
    public void SetLanguage(string languageCode)
    {
        if (!SupportedLanguages.Contains(languageCode))
        {
            return;
        }

        CurrentLanguageCode = languageCode;

        // Update resource dictionary
        UpdateResourceDictionary(languageCode);

        // Notify indexer bindings that localized values changed.
        OnIndexerChanged();
    }

    // Enables bindings like ["Label.Layers"] from XAML markup extension.
    // Accessed indirectly by WPF through Binding path syntax (e.g. "[Some.Key]").
    // ReSharper disable once UnusedMember.Global
    public string this[string key] => GetString(key);
    
    /// Creates a one-way binding to a localized string key via the indexer path (e.g. [Label.Layers]).
    public static Binding CreateBinding(string key, string? fallbackValue = null)
    {
        string fallback = string.IsNullOrWhiteSpace(fallbackValue) ? key : fallbackValue;
        return new Binding($"[{key}]")
        {
            Source = Instance,
            Mode = BindingMode.OneWay,
            FallbackValue = fallback,
            TargetNullValue = fallback
        };
    }
    
    /// Gets a localized string for the given key.
    private static string GetString(string key)
    {
        try
        {
            object? resource = Application.Current.Resources[key];
            return resource?.ToString() ?? key;
        }
        catch
        {
            return key;
        }
    }
    
    /// Loads the language-specific resource dictionary.
    private static void UpdateResourceDictionary(string languageCode)
    {
        try
        {
            string resourceUri = $"pack://application:,,,/Resources/Strings.{languageCode}.xaml";
            ResourceDictionary newDictionary = new() { Source = new Uri(resourceUri) };

            // Remove old language dictionary
            ResourceDictionary? oldDict = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.OriginalString.Contains("Strings.") ?? false);
                
            if (oldDict != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(oldDict);
            }

            // Add new language dictionary
            Application.Current.Resources.MergedDictionaries.Insert(0, newDictionary);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load resource dictionary: {ex.Message}");
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnIndexerChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
    }
}