using System.IO;
using System.Text.Json;
using SimpleSpoutOverlay.Models;

namespace SimpleSpoutOverlay.Services;

/// Handles saving and loading session files from disk.
public sealed class SessionPersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    public static string DefaultSessionPath
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "SimpleSpoutOverlay", "session.json");
        }
    }

    private static string LastSavedSetupPathFile
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "SimpleSpoutOverlay", "last-saved-setup-path.txt");
        }
    }

    public static SessionState? LoadFromPath(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        string json = File.ReadAllText(path);
        SessionState? state = JsonSerializer.Deserialize<SessionState>(json, JsonOptions);

        return state is not { Version: > 0 } ? null : state;
    }

    public static void SaveToPath(string path, SessionState state)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static void SaveLastSetupPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(LastSavedSetupPathFile);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(LastSavedSetupPathFile, fullPath);
    }

    public static string? LoadLastSetupPath()
    {
        if (!File.Exists(LastSavedSetupPathFile))
        {
            return null;
        }

        string value = File.ReadAllText(LastSavedSetupPathFile).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}


