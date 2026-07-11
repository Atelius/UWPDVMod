using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace UWPDVMod.Core;

/// <summary>Finds a game's install folder from Steam and remembers the user's choice per profile.</summary>
public static class GameLocator
{
    private static string ConfigDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UWPDVMod");
    private static string ConfigFile => Path.Combine(ConfigDir, "config.json");

    public static bool IsValidGameFolder(GameSpec game, string folder)
        => !string.IsNullOrWhiteSpace(folder) && File.Exists(Path.Combine(folder, game.ExeFileName));

    /// <summary>Steam registry -> libraryfolders.vdf -> each library's common\{game} folder.</summary>
    public static string? AutoDetect(GameSpec game)
    {
        if (Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SteamPath", null) is not string steamPath)
            return null;
        steamPath = steamPath.Replace('/', '\\');

        var libraries = new List<string> { steamPath };
        string vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdf))
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"(?<p>[^\"]+)\""))
                libraries.Add(m.Groups["p"].Value.Replace("\\\\", "\\"));
        }

        foreach (string lib in libraries.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string candidate = Path.Combine(lib, "steamapps", "common", game.SteamFolderName);
            if (IsValidGameFolder(game, candidate)) return candidate;
        }
        return null;
    }

    public static string? LoadSavedFolder(string profileId)
    {
        try
        {
            if (!File.Exists(ConfigFile)) return null;
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(ConfigFile));
            return map != null && map.TryGetValue(profileId, out var f) ? f : null;
        }
        catch { return null; }
    }

    public static void SaveFolder(string profileId, string folder)
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            Dictionary<string, string> map = File.Exists(ConfigFile)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(ConfigFile)) ?? new()
                : new();
            map[profileId] = folder;
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* non-fatal */ }
    }
}
