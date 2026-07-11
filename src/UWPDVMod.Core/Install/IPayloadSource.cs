using System.Reflection;

namespace UWPDVMod.Core;

/// <summary>Supplies the mod files the installer copies into the game folder.</summary>
public interface IPayloadSource
{
    /// <summary>True when every payload file is available.</summary>
    bool Available { get; }

    /// <summary>Opens a readable stream for the file at <paramref name="relativePath"/>, or null.</summary>
    Stream? Open(string relativePath);
}

/// <summary>
/// Reads payload files embedded as assembly resources, so the published app is a single
/// self-contained exe. Matches resources by leaf filename under a profile's resource folder.
/// </summary>
public sealed class EmbeddedPayloadSource : IPayloadSource
{
    private readonly Assembly _asm;
    private readonly string _idToken;
    private readonly IReadOnlyList<string> _requiredLeaves;
    private readonly string[] _names;

    /// <param name="asm">Assembly holding the embedded resources (UWPDVMod.Core).</param>
    /// <param name="profileId">Profile id, e.g. "granblue-relink" (dashes match embedded '_').</param>
    /// <param name="requiredLeaves">Leaf filenames that must all be present for Available.</param>
    public EmbeddedPayloadSource(Assembly asm, string profileId, IReadOnlyList<string> requiredLeaves)
    {
        _asm = asm;
        _idToken = "." + profileId.Replace('-', '_') + ".";
        _requiredLeaves = requiredLeaves;
        _names = asm.GetManifestResourceNames();
    }

    public bool Available => _requiredLeaves.All(leaf => Resolve(leaf) != null);

    public Stream? Open(string relativePath)
    {
        string leaf = relativePath.Replace('\\', '/').Split('/')[^1];
        string? name = Resolve(leaf);
        return name == null ? null : _asm.GetManifestResourceStream(name);
    }

    private string? Resolve(string leaf) =>
        _names.FirstOrDefault(n =>
            n.Contains(_idToken, StringComparison.OrdinalIgnoreCase) &&
            n.EndsWith("." + leaf, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Reads payload files from a folder on disk (used for dev builds without embedding).</summary>
public sealed class FolderPayloadSource : IPayloadSource
{
    private readonly string _dir;
    private readonly IReadOnlyList<string> _requiredLeaves;

    public FolderPayloadSource(string dir, IReadOnlyList<string> requiredLeaves)
    {
        _dir = dir;
        _requiredLeaves = requiredLeaves;
    }

    public bool Available => Directory.Exists(_dir) && _requiredLeaves.All(l => File.Exists(Path.Combine(_dir, l)));

    public Stream? Open(string relativePath)
    {
        string leaf = relativePath.Replace('\\', '/').Split('/')[^1];
        string p = Path.Combine(_dir, leaf);
        return File.Exists(p) ? File.OpenRead(p) : null;
    }
}
