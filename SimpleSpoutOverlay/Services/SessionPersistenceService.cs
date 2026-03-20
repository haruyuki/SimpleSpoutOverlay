using System.IO;
using System.Text.Json;
using SimpleSpoutOverlay.Models;

namespace SimpleSpoutOverlay.Services;

/// <summary>
/// Handles saving and loading session files from disk.
/// </summary>
public sealed class SessionPersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string DefaultSessionPath
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "SimpleSpoutOverlay", "session.json");
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
}


