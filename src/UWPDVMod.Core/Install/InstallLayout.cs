namespace UWPDVMod.Core;

/// <summary>Where the mod's files live inside the game folder.</summary>
public class InstallLayout
{
    /// <summary>ASI loader relative to the game root, e.g. "winmm.dll".</summary>
    public required string LoaderRelativePath { get; init; }

    /// <summary>Suffix appended to disable the loader without deleting it.</summary>
    public string DisabledSuffix { get; init; } = ".disabled";

    /// <summary>The mod DLL, e.g. "scripts\GBFRelinkFix.asi". Used to detect an install.</summary>
    public required string AsiRelativePath { get; init; }

    /// <summary>The settings file, e.g. "scripts\GBFRelinkFix.ini".</summary>
    public required string IniRelativePath { get; init; }

    /// <summary>Optional log file removed on uninstall, e.g. "scripts\GBFRelinkFix.log".</summary>
    public string? LogRelativePath { get; init; }

    /// <summary>
    /// Every file the installer lays down (relative paths). Includes the loader, the asi
    /// and the default ini. The payload source is keyed by these same relative paths.
    /// </summary>
    public required IReadOnlyList<string> PayloadFiles { get; init; }
}
