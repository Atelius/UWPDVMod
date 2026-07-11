namespace UWPDVMod.Core;

/// <summary>
/// A complete, data-driven description of one mod: how to find its game, how it installs,
/// which settings it exposes (schema drives the whole UI), and how to recommend settings
/// from detected hardware. Add a new game by adding a new implementation.
/// </summary>
public interface IModProfile
{
    /// <summary>Stable id, kebab-case, e.g. "granblue-relink". Used for paths and resource matching.</summary>
    string Id { get; }

    /// <summary>Human-readable name shown in the profile selector.</summary>
    string DisplayName { get; }

    GameSpec Game { get; }
    InstallLayout Install { get; }
    IPayloadSource Payload { get; }

    /// <summary>Setting groups → tabs; each setting → one control. Drives the entire settings UI.</summary>
    IReadOnlyList<SettingGroup> Schema { get; }

    /// <summary>Game-specific mapping from detected hardware to recommended setting values.</summary>
    IReadOnlyList<Recommendation> Recommend(HardwareInfo hw);
}
