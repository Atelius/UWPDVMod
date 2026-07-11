using System.Diagnostics;
using System.Globalization;
using UWPDVMod.Core;

namespace UWPDVMod.App;

public class MainForm : Form
{
    private readonly ComboBox _cmbProfile = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
    private readonly TextBox _txtFolder = new();
    private readonly Button _btnBrowse = new() { Text = "Browse...", AutoSize = true };
    private readonly Button _btnDetect = new() { Text = "Auto-detect", AutoSize = true };
    private readonly Label _lblStatus = new() { AutoSize = true, Font = new Font(DefaultFont, FontStyle.Bold), UseMnemonic = false };
    private readonly Button _btnInstall = new() { Text = "Install", AutoSize = true };
    private readonly Button _btnEnable = new() { Text = "Enable", AutoSize = true };
    private readonly Button _btnDisable = new() { Text = "Disable", AutoSize = true };
    private readonly Button _btnUninstall = new() { Text = "Uninstall", AutoSize = true };

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly Button _btnAnalyze = new() { Text = "🔍  Analyze my PC", Width = 150, Height = 32 };
    private readonly Button _btnReload = new() { Text = "Reload", Width = 90, Height = 32 };
    private readonly Button _btnSave = new() { Text = "Save settings", Width = 120, Height = 32 };
    private readonly Label _lblFooter = new() { AutoSize = true, ForeColor = SystemColors.GrayText };
    private readonly ToolTip _tips = new() { AutoPopDelay = 20000, InitialDelay = 400 };

    private IModProfile _profile = ProfileRegistry.All[0];
    private ModInstaller? _installer;
    private IniDocument? _ini;
    private readonly List<SettingControlFactory.Bound> _bound = new();
    private readonly Dictionary<(string, string), SettingControlFactory.Bound> _byKey = new();

    public MainForm()
    {
        Text = "Ultrawide Mod Configurator";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(720, 580);

        Controls.Add(BuildLayout());
        WireStaticEvents();

        foreach (var p in ProfileRegistry.All) _cmbProfile.Items.Add(p.DisplayName);
        _cmbProfile.SelectedIndex = 0;
        LoadProfile(ProfileRegistry.All[0]);
    }

    // ---------- static layout skeleton ----------

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var grp = new GroupBox { Text = "Game && mod", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
        var inner = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1 };

        var profileRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        profileRow.Controls.Add(new Label { Text = "Mod:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) });
        profileRow.Controls.Add(_cmbProfile);
        inner.Controls.Add(profileRow);

        var folderRow = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, Margin = new Padding(0, 4, 0, 0) };
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _txtFolder.Dock = DockStyle.Fill;
        folderRow.Controls.Add(_txtFolder, 0, 0);
        folderRow.Controls.Add(_btnBrowse, 1, 0);
        folderRow.Controls.Add(_btnDetect, 2, 0);
        inner.Controls.Add(folderRow);

        var statusRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
        statusRow.Controls.Add(new Label { Text = "Status:", AutoSize = true, Padding = new Padding(0, 5, 4, 0) });
        _lblStatus.Padding = new Padding(0, 5, 12, 0);
        statusRow.Controls.Add(_lblStatus);
        statusRow.Controls.Add(_btnInstall);
        statusRow.Controls.Add(_btnEnable);
        statusRow.Controls.Add(_btnDisable);
        statusRow.Controls.Add(_btnUninstall);
        inner.Controls.Add(statusRow);

        grp.Controls.Add(inner);
        root.Controls.Add(grp, 0, 0);
        root.Controls.Add(_tabs, 0, 1);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, ColumnCount = 4, Padding = new Padding(0, 8, 0, 0) };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.Controls.Add(_btnAnalyze, 0, 0);
        _lblFooter.Anchor = AnchorStyles.Left;
        bottom.Controls.Add(_lblFooter, 1, 0);
        bottom.Controls.Add(_btnReload, 2, 0);
        bottom.Controls.Add(_btnSave, 3, 0);
        root.Controls.Add(bottom, 0, 2);

        _tips.SetToolTip(_txtFolder, "The game's installation folder.");
        _tips.SetToolTip(_btnDetect, "Search your Steam libraries for the game automatically.");
        _tips.SetToolTip(_btnInstall, "Copy the mod files into the game folder.");
        _tips.SetToolTip(_btnEnable, "Re-activate the mod by restoring the ASI loader.");
        _tips.SetToolTip(_btnDisable, "Deactivate the mod without losing your settings.");
        _tips.SetToolTip(_btnUninstall, "Remove all mod files from the game folder. Settings are backed up first.");
        _tips.SetToolTip(_btnAnalyze, "Detect your monitor and GPU, then propose the best settings. Nothing changes until you confirm.");
        return root;
    }

    private void WireStaticEvents()
    {
        _cmbProfile.SelectedIndexChanged += (_, _) => LoadProfile(ProfileRegistry.All[_cmbProfile.SelectedIndex]);
        _btnBrowse.Click += (_, _) => BrowseFolder();
        _btnDetect.Click += (_, _) =>
        {
            string? found = GameLocator.AutoDetect(_profile.Game);
            if (found == null) { MessageBox.Show(this, $"Could not find {_profile.Game.SteamFolderName} in any Steam library.", Text); return; }
            _txtFolder.Text = found;
            OnFolderChanged();
        };
        _txtFolder.Leave += (_, _) => OnFolderChanged();

        _btnInstall.Click += (_, _) => DoInstall();
        _btnEnable.Click += (_, _) => RunAction("enable", () => _installer!.Enable());
        _btnDisable.Click += (_, _) => RunAction("disable", () => _installer!.Disable());
        _btnUninstall.Click += (_, _) => DoUninstall();

        _btnReload.Click += (_, _) => { LoadIni(); Footer("Settings reloaded from disk."); };
        _btnSave.Click += (_, _) => SaveIni();
        _btnAnalyze.Click += (_, _) => Analyze();
    }

    // ---------- profile / schema-driven tabs ----------

    private void LoadProfile(IModProfile profile)
    {
        _profile = profile;
        BuildTabs();

        string folder = GameLocator.LoadSavedFolder(profile.Id) ?? GameLocator.AutoDetect(profile.Game) ?? "";
        _txtFolder.Text = folder;
        RefreshModState();
        LoadIni();
    }

    private void BuildTabs()
    {
        _tabs.TabPages.Clear();
        _bound.Clear();
        _byKey.Clear();

        foreach (var group in _profile.Schema)
        {
            var page = new TabPage(group.Title);
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true, Padding = new Padding(12), AutoScroll = true };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            foreach (var def in group.Settings)
            {
                var b = SettingControlFactory.Build(def);
                _bound.Add(b);
                _byKey[(def.Section, def.Key)] = b;
                if (!string.IsNullOrEmpty(def.Tooltip)) _tips.SetToolTip(b.Editor, def.Tooltip);

                int r = t.RowCount++;
                t.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                if (b.BoolBox != null)
                {
                    // checkbox carries its own label; span both columns
                    b.Editor.Margin = new Padding(3, 6, 10, 3);
                    t.Controls.Add(b.Editor, 0, r);
                    t.SetColumnSpan(b.Editor, 2);
                    if (!string.IsNullOrEmpty(def.Tooltip)) _tips.SetToolTip(b.BoolBox, def.Tooltip);
                }
                else
                {
                    var lbl = new Label { Text = def.Label + ":", AutoSize = true, Margin = new Padding(3, 8, 10, 3) };
                    if (!string.IsNullOrEmpty(def.Tooltip)) _tips.SetToolTip(lbl, def.Tooltip);
                    t.Controls.Add(lbl, 0, r);
                    t.Controls.Add(WithHint(b.Editor, def.InlineHint), 1, r);
                }

                if (b.BoolBox != null)
                    b.BoolBox.CheckedChanged += (_, _) => ApplyDependencies();
            }

            page.Controls.Add(t);
            _tabs.TabPages.Add(page);
        }
    }

    private static Control WithHint(Control editor, string hint)
    {
        editor.Margin = new Padding(3, 4, 3, 3);
        if (string.IsNullOrEmpty(hint)) return editor;
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        row.Controls.Add(editor);
        row.Controls.Add(new Label { Text = hint, AutoSize = true, ForeColor = SystemColors.GrayText, Padding = new Padding(8, 7, 0, 0) });
        return row;
    }

    private void ApplyDependencies()
    {
        foreach (var b in _bound)
        {
            if (b.Def.EnabledWhenKey == null) continue;
            var key = (b.Def.EnabledWhenSection ?? b.Def.Section, b.Def.EnabledWhenKey);
            if (_byKey.TryGetValue(key, out var driver) && driver.BoolBox != null)
                b.Editor.Enabled = driver.BoolBox.Checked;
        }
    }

    // ---------- folder / install state ----------

    private void BrowseFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = $"Select the {_profile.Game.SteamFolderName} game folder",
            UseDescriptionForTitle = true,
            SelectedPath = GameLocator.IsValidGameFolder(_profile.Game, _txtFolder.Text) ? _txtFolder.Text : "",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (!GameLocator.IsValidGameFolder(_profile.Game, dlg.SelectedPath))
        {
            MessageBox.Show(this, $"That folder does not contain {_profile.Game.ExeFileName}.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _txtFolder.Text = dlg.SelectedPath;
        OnFolderChanged();
    }

    private void OnFolderChanged()
    {
        RefreshModState();
        LoadIni();
        if (GameLocator.IsValidGameFolder(_profile.Game, _txtFolder.Text))
            GameLocator.SaveFolder(_profile.Id, _txtFolder.Text);
    }

    private void RefreshModState()
    {
        bool valid = GameLocator.IsValidGameFolder(_profile.Game, _txtFolder.Text);
        _installer = valid ? new ModInstaller(_profile, _txtFolder.Text) : null;

        ModState? state = _installer?.GetState();
        (_lblStatus.Text, _lblStatus.ForeColor) = state switch
        {
            ModState.Enabled => ("Installed & enabled", Color.Green),
            ModState.Disabled => ("Installed, disabled", Color.DarkOrange),
            ModState.NotInstalled => ("Not installed", Color.Firebrick),
            _ => ("No game folder selected", SystemColors.GrayText),
        };

        _btnInstall.Text = state == ModState.NotInstalled ? "Install" : "Reinstall";
        _btnInstall.Enabled = valid && (_installer?.PayloadAvailable ?? false);
        _btnEnable.Enabled = state == ModState.Disabled;
        _btnDisable.Enabled = state == ModState.Enabled;
        _btnUninstall.Enabled = state is ModState.Enabled or ModState.Disabled;

        if (valid && _installer is { PayloadAvailable: false } && state == ModState.NotInstalled)
            Footer("Install unavailable: mod payload not bundled in this build.");
        else if (IsGameRunning())
            Footer("Game is running — saved changes apply on the next launch.");
        else
            Footer("");
    }

    private bool IsGameRunning() => Process.GetProcessesByName(_profile.Game.ProcessName).Length > 0;

    private void RunAction(string action, Action work, bool restore = false)
    {
        if (_installer == null) return;
        try
        {
            work();
            Footer($"Mod {action}d.");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            if (MessageBox.Show(this,
                    $"Windows denied access to the game folder:\n{ex.Message}\n\nRetry as administrator?",
                    "Administrator rights required", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try { _installer.RelaunchElevated(action, restore); Footer($"Mod {action}d (elevated)."); }
                catch (Exception ex2) { MessageBox.Show(this, ex2.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        RefreshModState();
        LoadIni();
    }

    private void DoInstall()
    {
        if (_installer == null) return;
        bool restore = false;
        if (_installer.HasBackupIni && !_installer.HasIni)
            restore = MessageBox.Show(this,
                "A backup of your previous settings exists.\nRestore it instead of the default settings?",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        RunAction("install", () => _installer.Install(restore), restore);
    }

    private void DoUninstall()
    {
        if (_installer == null) return;
        if (MessageBox.Show(this, "Remove the mod from the game folder?\nYour settings will be backed up first.",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        RunAction("uninstall", () => _installer.Uninstall());
    }

    // ---------- ini <-> controls ----------

    private void LoadIni()
    {
        _ini = null;
        string? path = _installer?.IniPath;
        if (path != null && File.Exists(path))
        {
            try { _ini = new IniDocument(path); }
            catch (Exception ex) { MessageBox.Show(this, $"Could not read settings:\n{ex.Message}", Text); }
        }

        bool have = _ini != null;
        _tabs.Enabled = have;
        _btnSave.Enabled = _btnReload.Enabled = _btnAnalyze.Enabled = have;
        if (!have) return;

        foreach (var b in _bound)
            b.SetValue(_ini!.Get(b.Def.Section, b.Def.Key) ?? b.Def.Default);
        ApplyDependencies();
    }

    private void SaveIni()
    {
        if (_ini == null) return;
        try
        {
            foreach (var b in _bound)
            {
                string newVal = b.GetValue();
                if (!ValuesEqual(_ini.Get(b.Def.Section, b.Def.Key), newVal))
                    _ini.Set(b.Def.Section, b.Def.Key, newVal);
            }
            _ini.Save();
            Footer(IsGameRunning()
                ? "Settings saved — the running game applies them on next launch."
                : "Settings saved.");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            MessageBox.Show(this, $"Could not write the settings file:\n{ex.Message}\n\nTry running as administrator.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Footer(string text) => _lblFooter.Text = text;

    // ---------- analyze ----------

    private void Analyze()
    {
        if (_ini == null) return;
        HardwareInfo hw;
        try { hw = SystemAnalyzer.Detect(); }
        catch (Exception ex) { MessageBox.Show(this, $"Hardware detection failed:\n{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

        var recs = _profile.Recommend(hw);
        var changed = recs.Where(r => !ValuesEqual(_ini.Get(r.Section, r.Key), r.NewValue)).ToList();

        string hzText = hw.MaxRefreshRate > hw.RefreshRate
            ? $"{hw.RefreshRate} Hz (panel supports {hw.MaxRefreshRate} Hz)"
            : $"{hw.RefreshRate} Hz";
        string hwText =
            $"Display:  {hw.ScreenWidth} × {hw.ScreenHeight} @ {hzText}\n" +
            $"GPU:      {hw.GpuName}  ({hw.VramGb:0.#} GB VRAM)\n" +
            $"RAM:      {hw.RamGb:0.#} GB\n\n";

        if (changed.Count == 0)
        {
            MessageBox.Show(this, hwText + "Your settings already match the recommendation — nothing to change.",
                "Analyze my PC", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string changes = string.Join("\n", changed.Select(r =>
            $"  • [{r.Section}] {r.Key}:  {_ini.Get(r.Section, r.Key) ?? "(unset)"}  →  {r.NewValue}\n      {r.Reason}"));

        if (MessageBox.Show(this, hwText + "Recommended changes:\n\n" + changes + "\n\nApply these settings?",
                "Analyze my PC", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        foreach (var r in changed) _ini.Set(r.Section, r.Key, r.NewValue);
        try
        {
            _ini.Save();
            LoadIni();
            Footer($"Applied {changed.Count} recommended setting(s).");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            MessageBox.Show(this, $"Could not write the settings file:\n{ex.Message}\n\nTry running as administrator.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static bool ValuesEqual(string? current, string proposed)
    {
        if (current == null) return false;
        if (current.Equals(proposed, StringComparison.OrdinalIgnoreCase)) return true;
        return float.TryParse(current, NumberStyles.Any, CultureInfo.InvariantCulture, out float a)
            && float.TryParse(proposed, NumberStyles.Any, CultureInfo.InvariantCulture, out float b)
            && Math.Abs(a - b) < 0.0001f;
    }
}
