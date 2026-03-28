namespace SimpleSpoutOverlay;

using Services;
using System.Windows;

/// Interaction logic for App.xaml
public partial class App
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		// Initialize localization with saved language preference
		string savedLanguage = SessionPersistenceService.LoadLanguagePreference();
		LocalizationService.Instance.SetLanguage(savedLanguage);
	}
}