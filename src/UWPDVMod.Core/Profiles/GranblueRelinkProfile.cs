namespace UWPDVMod.Core;

/// <summary>
/// Profile for Granblue Fantasy: Relink + the GBFRelinkFix mod.
/// All Granblue-specific data (paths, ini schema, tooltips, presets, recommendation rules)
/// lives here; the engine and UI are game-agnostic.
/// </summary>
public sealed class GranblueRelinkProfile : IModProfile
{
    public string Id => "granblue-relink";
    public string DisplayName => "Granblue Fantasy: Relink — GBFRelinkFix";

    public GameSpec Game { get; } = new()
    {
        ExeFileName = "granblue_fantasy_relink.exe",
        SteamFolderName = "Granblue Fantasy Relink",
        SteamAppId = "881020",
    };

    public InstallLayout Install { get; } = new()
    {
        LoaderRelativePath = "winmm.dll",
        AsiRelativePath = @"scripts\GBFRelinkFix.asi",
        IniRelativePath = @"scripts\GBFRelinkFix.ini",
        LogRelativePath = @"scripts\GBFRelinkFix.log",
        PayloadFiles = new[] { "winmm.dll", @"scripts\GBFRelinkFix.asi", @"scripts\GBFRelinkFix.ini" },
    };

    public IPayloadSource Payload { get; } = new EmbeddedPayloadSource(
        typeof(GranblueRelinkProfile).Assembly, "granblue-relink",
        new[] { "winmm.dll", "GBFRelinkFix.asi", "GBFRelinkFix.ini" });

    // ---- ini schema -> tabs & controls (tooltip text mirrors the ini comments) ----

    public IReadOnlyList<SettingGroup> Schema { get; } = new SettingGroup[]
    {
        new()
        {
            Title = "Display / Ultrawide",
            Settings = new SettingDef[]
            {
                new() { Section = "Custom Resolution", Key = "Enabled", Kind = ControlKind.Bool, Default = "true",
                    Label = "Custom resolution",
                    Tooltip = "Run the game at any resolution, including ultrawide. Leave width/height at 0 to use your desktop resolution." },
                new() { Section = "Custom Resolution", Key = "Width", Kind = ControlKind.Int, Default = "0",
                    Label = "Width", Min = 0, Max = 15360, Step = 20, InlineHint = "0 = desktop width",
                    EnabledWhenSection = "Custom Resolution", EnabledWhenKey = "Enabled",
                    Tooltip = "Horizontal resolution in pixels. 0 = use your desktop width." },
                new() { Section = "Custom Resolution", Key = "Height", Kind = ControlKind.Int, Default = "0",
                    Label = "Height", Min = 0, Max = 8640, Step = 20, InlineHint = "0 = desktop height",
                    EnabledWhenSection = "Custom Resolution", EnabledWhenKey = "Enabled",
                    Tooltip = "Vertical resolution in pixels. 0 = use your desktop height." },

                new() { Section = "Fix HUD", Key = "Enabled", Kind = ControlKind.Bool, Default = "true",
                    Label = "Fix HUD (scale to 16:9, span fades)",
                    Tooltip = "Resizes the HUD to 16:9 and spans backgrounds (loading fades etc.) across the screen." },

                new() { Section = "Span HUD", Key = "Enabled", Kind = ControlKind.Bool, Default = "true",
                    Label = "Span gameplay HUD",
                    Tooltip = "Spreads the gameplay HUD (health, minimap...) toward the screen edges." },
                new() { Section = "Span HUD", Key = "AspectRatio", Kind = ControlKind.EnumEditable, Default = "0",
                    Label = "HUD aspect ratio",
                    EnabledWhenSection = "Span HUD", EnabledWhenKey = "Enabled",
                    Tooltip = "Auto = HUD matches your screen's aspect ratio.\nPick 16:9 to keep the classic centered HUD shape while playing ultrawide, or type a custom ratio.",
                    Options = new EnumOption[]
                    {
                        new("0", "Auto (match screen)"),
                        new("1.7778", "16:9 (1.7778)"),
                        new("2.38888", "21:9 (2.3889)"),
                        new("3.5537", "32:9 (3.5537)"),
                    } },
                new() { Section = "Span HUD", Key = "ExcludeFramedMenus", Kind = ControlKind.Bool, Default = "true",
                    Label = "Fix framed menus (e.g. trait screen)",
                    Tooltip = "Fixed mode (recommended): keeps framed sub-menus like the weapon-trait screen from shifting when the HUD is spanned.\nUncheck for the original look (trait summary box sits slightly off)." },
                new() { Section = "Span HUD", Key = "SpanAllHUD", Kind = ControlKind.Bool, Default = "false",
                    Label = "Span ALL HUD (menus etc.)",
                    Tooltip = "Force ALL HUD elements (menus etc.) to span, overriding the framed-menu fix. May cause visual issues." },
                new() { Section = "Span HUD", Key = "SpanAllBackgrounds", Kind = ControlKind.Bool, Default = "false",
                    Label = "Span ALL backgrounds",
                    Tooltip = "Span all background images (main menu backdrop etc.). May cause visual issues." },

                new() { Section = "Fix Aspect Ratio", Key = "Enabled", Kind = ControlKind.Bool, Default = "true",
                    Label = "Fix aspect ratio (<16:9)",
                    Tooltip = "Fixes the rendered aspect ratio on displays narrower than 16:9." },
                new() { Section = "Fix FOV", Key = "Enabled", Kind = ControlKind.Bool, Default = "true",
                    Label = "Fix FOV (<16:9)",
                    Tooltip = "Fixes the field of view on displays narrower than 16:9." },
            },
        },
        new()
        {
            Title = "Gameplay",
            Settings = new SettingDef[]
            {
                new() { Section = "Gameplay FOV", Key = "Multiplier", Kind = ControlKind.Float, Default = "1",
                    Label = "FOV multiplier", Min = 0.1m, Max = 2.5m, Step = 0.05m, Decimals = 2,
                    InlineHint = "1.0 = original, 1.2 = 20% wider",
                    Tooltip = "Gameplay field of view. 1.0 = original, 1.2 = 20% higher FOV." },
                new() { Section = "Gameplay Camera Distance", Key = "Multiplier", Kind = ControlKind.Float, Default = "1",
                    Label = "Camera distance multiplier", Min = 0.1m, Max = 2.5m, Step = 0.05m, Decimals = 2,
                    InlineHint = "1.0 = original, 1.2 = 20% further back",
                    Tooltip = "Gameplay camera distance. 1.0 = original, 1.2 = 20% further back." },
            },
        },
        new()
        {
            Title = "Graphics",
            Settings = new SettingDef[]
            {
                new() { Section = "Shadow Quality", Key = "Enabled", Kind = ControlKind.Bool, Default = "false",
                    Label = "Override shadow quality",
                    Tooltip = "Override shadow map resolution. Higher = sharper shadows, more VRAM.\n" +
                              "WARNING: currently unsafe on the Endless Ragnarok (2.0.2) build - crashes the game on launch. Leave disabled." },
                new() { Section = "Shadow Quality", Key = "Value", Kind = ControlKind.Enum, Default = "4096",
                    Label = "Shadow resolution",
                    EnabledWhenSection = "Shadow Quality", EnabledWhenKey = "Enabled",
                    Tooltip = "Game default: ultra = 2048, high = 1024, standard = 256.",
                    Options = new EnumOption[]
                    {
                        new("256", "256"), new("1024", "1024"), new("2048", "2048"),
                        new("4096", "4096"), new("8192", "8192"),
                    } },
                new() { Section = "Level of Detail", Key = "Multiplier", Kind = ControlKind.Float, Default = "1",
                    Label = "LOD distance multiplier", Min = 0.1m, Max = 10m, Step = 0.1m, Decimals = 2,
                    InlineHint = "raise above 1.0 to reduce object pop-in",
                    Tooltip = "Distance at which objects pop in. Increase to reduce pop-in (costs performance)." },
                new() { Section = "Disable TAA", Key = "Enabled", Kind = ControlKind.Bool, Default = "false",
                    Label = "Disable TAA (temporal anti-aliasing)",
                    Tooltip = "Disables temporal anti-aliasing. Sharper image but more shimmering/aliasing." },
            },
        },
        new()
        {
            Title = "Performance",
            Settings = new SettingDef[]
            {
                new() { Section = "Raise Framerate Cap", Key = "Enabled", Kind = ControlKind.Bool, Default = "false",
                    Label = "Raise framerate cap",
                    Tooltip = "Experimental! Raises the game's framerate cap.\nPhysics glitches (cloth/hair) have been reported above 60fps.\nIf you see issues, drop the value to 60 or disable this." },
                new() { Section = "Raise Framerate Cap", Key = "Value", Kind = ControlKind.EnumEditable, Default = "240",
                    Label = "Framerate cap (fps)",
                    EnabledWhenSection = "Raise Framerate Cap", EnabledWhenKey = "Enabled",
                    Tooltip = "Target framerate cap, 30–240. Values above your monitor's refresh rate are wasted.",
                    Options = new EnumOption[]
                    {
                        new("30", "30"), new("60", "60"), new("120", "120"),
                        new("144", "144"), new("165", "165"), new("240", "240"),
                    } },
                new() { Section = "GBFRelinkFix Parameters", Key = "InjectionDelay", Kind = ControlKind.Int, Default = "1000",
                    Label = "Injection delay (ms)", Min = 0, Max = 30000, Step = 250,
                    Tooltip = "How long the fix waits after the game starts before installing its hooks. Leave at 1000 unless the mod fails to apply." },
            },
        },
    };

    // ---- hardware -> recommended settings ----

    private static readonly int[] CommonFps = { 240, 165, 144, 120, 60, 30 };

    public IReadOnlyList<Recommendation> Recommend(HardwareInfo hw)
    {
        var recs = new List<Recommendation>
        {
            new("Custom Resolution", "Enabled", "true", "Lets the fix drive the game resolution"),
            new("Custom Resolution", "Width", "0", "0 = use desktop resolution automatically"),
            new("Custom Resolution", "Height", "0", "0 = use desktop resolution automatically"),
        };

        if (hw.Aspect > 16f / 9f + 0.01f)
        {
            recs.Add(new("Fix HUD", "Enabled", "true", $"Display is wider than 16:9 ({hw.ScreenWidth}x{hw.ScreenHeight})"));
            recs.Add(new("Span HUD", "Enabled", "true", "Spread the gameplay HUD across the ultrawide screen"));
            recs.Add(new("Span HUD", "AspectRatio", "0", "0 = match your screen's aspect ratio"));
        }

        int panelHz = hw.PanelHz;
        if (panelHz > 60)
        {
            int fps = CommonFps.First(f => f <= panelHz);
            string note = hw.MaxRefreshRate > hw.RefreshRate
                ? $"Panel supports {hw.MaxRefreshRate} Hz (desktop currently at {hw.RefreshRate} Hz — consider raising it in Windows display settings)"
                : $"Monitor refresh is {panelHz} Hz";
            recs.Add(new("Raise Framerate Cap", "Enabled", "true", note));
            recs.Add(new("Raise Framerate Cap", "Value", fps.ToString(),
                $"Highest common cap not exceeding {panelHz} Hz (drop to 60 if physics glitch)"));
        }
        else
        {
            recs.Add(new("Raise Framerate Cap", "Enabled", "false", $"Monitor refresh is {panelHz} Hz; the game's own cap is fine"));
        }

        double vramGb = hw.VramGb;
        if (vramGb >= 12)
        {
            // Shadow Quality is intentionally NOT recommended: its signature is unsafe on
            // the Endless Ragnarok (2.0.2) build (corrupts memory, crashes on launch).
            recs.Add(new("Level of Detail", "Multiplier", "1.5", "Reduce object pop-in on a high-end GPU"));
        }

        return recs;
    }
}
