using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SimpleSpoutOverlay.Services
{
    /// <summary>
    /// Manages application localization and language switching.
    /// Supports runtime language changes without app restart.
    /// </summary>
    public sealed class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService? _instance;
        private CultureInfo _currentCulture;
        private string _currentLanguageCode;

        public event PropertyChangedEventHandler? PropertyChanged;

        public static LocalizationService Instance => _instance ??= new LocalizationService();

        // Supported languages
        public static readonly string[] SupportedLanguages = ["en-US", "zh-TW"];
        public static readonly Dictionary<string, string> LanguageNames = new()
        {
            { "en-US", "English" },
            { "zh-TW", "繁體中文" }
        };

        private LocalizationService()
        {
            _currentLanguageCode = "en-US";
            _currentCulture = new CultureInfo(_currentLanguageCode);
        }

        /// <summary>
        /// Gets the current language code (e.g., "en-US", "zh-TW").
        /// </summary>
        public string CurrentLanguageCode
        {
            get => _currentLanguageCode;
            private set
            {
                if (_currentLanguageCode == value) return;
                _currentLanguageCode = value;
                _currentCulture = new CultureInfo(value);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the current UI culture.
        /// </summary>
        public CultureInfo CurrentCulture => _currentCulture;

        /// <summary>
        /// Gets the list of available language codes.
        /// </summary>
        public IReadOnlyList<string> AvailableLanguages => SupportedLanguages;

        /// <summary>
        /// Switches to the specified language and updates all UI bindings.
        /// </summary>
        public void SetLanguage(string languageCode)
        {
            if (!SupportedLanguages.Contains(languageCode))
            {
                return;
            }

            CurrentLanguageCode = languageCode;

            // Update resource dictionary
            UpdateResourceDictionary(languageCode);

            // Notify all UI bindings
            OnPropertyChanged(nameof(CurrentLanguageCode));
            OnPropertyChanged("Item[]");
        }

        // Enables bindings like ["Label.Layers"] from XAML markup extension.
        public string this[string key] => GetString(key);

        /// <summary>
        /// Gets a localized string for the given key.
        /// </summary>
        public string GetString(string key)
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

        /// <summary>
        /// Loads the language-specific resource dictionary.
        /// </summary>
        private void UpdateResourceDictionary(string languageCode)
        {
            try
            {
                string resourceUri = $"pack://application:,,,/Resources/Strings.{languageCode}.xaml";
                ResourceDictionary newDictionary = new() { Source = new Uri(resourceUri) };

                // Remove old language dictionary
                var oldDict = Application.Current.Resources.MergedDictionaries
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
    }
}


