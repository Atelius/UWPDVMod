using System.Diagnostics;

namespace UWPDVMod.Core;

public enum ModState { NotInstalled, Enabled, Disabled }

/// <summary>
/// Manages a mod's files inside the game folder for a given profile:
///   loader (e.g. winmm.dll, renamed to *.disabled to deactivate),
///   the asi, the ini, and an optional log. Settings are backed up before removal.
/// </summary>
public class ModInstaller
{
    private readonly IModProfile _p;
    private readonly string _game;

    public ModInstaller(IModProfile profile, string gameFolder)
    {
        _p = profile;
        _game = gameFolder;
    }

    private string Loader => Path.Combine(_game, _p.Install.LoaderRelativePath);
    private string LoaderDisabled => Loader + _p.Install.DisabledSuffix;
    private string Asi => Path.Combine(_game, _p.Install.AsiRelativePath);
    public string IniPath => Path.Combine(_game, _p.Install.IniRelativePath);
    private string? LogPath => _p.Install.LogRelativePath is { } l ? Path.Combine(_game, l) : null;

    private static string BackupRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UWPDVMod", "backup");
    private string BackupIniDir => Path.Combine(BackupRoot, _p.Id);
    private string BackupIni => Path.Combine(BackupIniDir, Path.GetFileName(_p.Install.IniRelativePath));

    public ModState GetState()
    {
        if (File.Exists(LoaderDisabled) && File.Exists(Asi)) return ModState.Disabled;
        if (File.Exists(Loader) && File.Exists(Asi)) return ModState.Enabled;
        return ModState.NotInstalled;
    }

    public bool HasIni => File.Exists(IniPath);
    public bool HasBackupIni => File.Exists(BackupIni);
    public bool PayloadAvailable => _p.Payload.Available;

    public void Disable()
    {
        if (File.Exists(Loader)) File.Move(Loader, LoaderDisabled, overwrite: true);
    }

    public void Enable()
    {
        if (File.Exists(LoaderDisabled)) File.Move(LoaderDisabled, Loader, overwrite: true);
    }

    public void Uninstall()
    {
        if (File.Exists(IniPath))
        {
            Directory.CreateDirectory(BackupIniDir);
            File.Copy(IniPath, BackupIni, overwrite: true);
        }
        DeleteIfExists(Loader);
        DeleteIfExists(LoaderDisabled);
        foreach (string rel in _p.Install.PayloadFiles)
            DeleteIfExists(Path.Combine(_game, rel));
        if (LogPath != null) DeleteIfExists(LogPath);

        // remove any now-empty directories we created under the game folder (e.g. scripts\)
        var subDirs = _p.Install.PayloadFiles
            .Select(rel => Path.GetDirectoryName(rel))
            .Where(d => !string.IsNullOrEmpty(d))
            .Select(d => d!)
            .Distinct();
        foreach (string rel in subDirs)
        {
            string dir = Path.Combine(_game, rel);
            if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                Directory.Delete(dir);
        }
    }

    public void Install(bool restoreBackupIni)
    {
        if (!_p.Payload.Available)
            throw new FileNotFoundException(
                "Mod payload is not available (not embedded and not found on disk). " +
                "Run scripts/fetch-granblue-payload.ps1 and rebuild.");

        foreach (string rel in _p.Install.PayloadFiles)
        {
            string dest = Path.Combine(_game, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            bool isIni = string.Equals(rel, _p.Install.IniRelativePath, StringComparison.OrdinalIgnoreCase);
            if (isIni)
            {
                if (restoreBackupIni && HasBackupIni) { File.Copy(BackupIni, dest, overwrite: true); continue; }
                if (File.Exists(dest)) continue; // keep the user's existing settings on reinstall
            }

            using Stream src = _p.Payload.Open(rel)
                ?? throw new FileNotFoundException($"Payload missing file: {rel}");
            using FileStream dst = File.Create(dest);
            src.CopyTo(dst);
        }
        DeleteIfExists(LoaderDisabled); // ensure the freshly installed loader is active
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>
    /// Relaunches this app elevated to perform <paramref name="action"/> against the same
    /// game folder, and waits for it to finish. The UI layer decides when to call this
    /// (typically after catching <see cref="UnauthorizedAccessException"/>).
    /// </summary>
    public void RelaunchElevated(string action, bool restoreIni = false)
    {
        string exe = Environment.ProcessPath!;
        string args = $"--elevated {action} {_p.Id} \"{_game}\"" + (restoreIni ? " restore" : "");
        var psi = new ProcessStartInfo(exe, args) { UseShellExecute = true, Verb = "runas" };
        using var proc = Process.Start(psi);
        proc?.WaitForExit();
    }
}
