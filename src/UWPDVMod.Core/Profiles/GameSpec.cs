namespace UWPDVMod.Core;

/// <summary>How to locate a game's install folder.</summary>
public class GameSpec
{
    /// <summary>Executable that must exist in the folder, e.g. "granblue_fantasy_relink.exe".</summary>
    public required string ExeFileName { get; init; }

    /// <summary>Folder name under steamapps\common, e.g. "Granblue Fantasy Relink".</summary>
    public required string SteamFolderName { get; init; }

    /// <summary>Optional Steam AppID (reserved for future use).</summary>
    public string? SteamAppId { get; init; }

    public string ProcessName => System.IO.Path.GetFileNameWithoutExtension(ExeFileName);
}
