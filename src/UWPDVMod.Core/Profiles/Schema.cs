namespace UWPDVMod.Core;

/// <summary>How a setting is presented and edited in the UI.</summary>
public enum ControlKind
{
    Bool,          // checkbox
    Int,           // numeric up/down (whole numbers)
    Float,         // numeric up/down (decimals)
    Enum,          // fixed dropdown (pick one preset)
    EnumEditable,  // dropdown with presets but free typing allowed (e.g. custom fps / aspect)
}

/// <summary>A dropdown choice: the value written to the ini and the label shown to the user.</summary>
public record EnumOption(string Value, string Label);

/// <summary>Describes one ini key and how to edit it. Pure data (no WinForms types).</summary>
public class SettingDef
{
    public required string Section { get; init; }
    public required string Key { get; init; }
    public required ControlKind Kind { get; init; }
    public required string Label { get; init; }
    public string Tooltip { get; init; } = "";
    public string InlineHint { get; init; } = "";
    public string Default { get; init; } = "";

    // numeric (Int/Float) parameters
    public decimal Min { get; init; } = 0m;
    public decimal Max { get; init; } = 100000m;
    public decimal Step { get; init; } = 1m;
    public int Decimals { get; init; } = 0;

    // Enum / EnumEditable options
    public IReadOnlyList<EnumOption> Options { get; init; } = Array.Empty<EnumOption>();

    // Optional dependency: control is enabled only while the referenced bool key is true.
    public string? EnabledWhenSection { get; init; }
    public string? EnabledWhenKey { get; init; }
}

/// <summary>A tab-worth of settings.</summary>
public class SettingGroup
{
    public required string Title { get; init; }
    public required IReadOnlyList<SettingDef> Settings { get; init; }
}
