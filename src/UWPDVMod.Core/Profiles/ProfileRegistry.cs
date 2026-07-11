namespace UWPDVMod.Core;

/// <summary>The set of built-in mod profiles. Add a game by adding its profile here.</summary>
public static class ProfileRegistry
{
    public static IReadOnlyList<IModProfile> All { get; } = new IModProfile[]
    {
        new GranblueRelinkProfile(),
    };

    public static IModProfile? ById(string id) =>
        All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
