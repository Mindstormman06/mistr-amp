using System.Text.Json;
using Stramp.Core.Settings;

namespace Stramp.Core.Playback;

/// <summary>
/// Loads and atomically saves the listening history database as JSON under the stramp data directory.
/// </summary>
public sealed class ListeningHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath;

    public ListeningHistoryStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(SettingsService.ConfigDirectory, "listening-history.json");
    }

    public string FilePath => _filePath;

    public ListeningHistoryDatabase Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var db = JsonSerializer.Deserialize<ListeningHistoryDatabase>(json);
                if (db is not null)
                {
                    db.Months ??= [];
                    return db;
                }
            }
        }
        catch
        {
            // Corrupt or unreadable db: return a fresh instance rather than crashing
        }

        return new ListeningHistoryDatabase();
    }

    public void Save(ListeningHistoryDatabase database)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("Listening history path must have a parent directory.");

        Directory.CreateDirectory(directory);
        var temporaryPath = _filePath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(database, JsonOptions));
            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
