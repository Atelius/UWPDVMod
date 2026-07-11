using System.Globalization;
using UWPDVMod.Core;

namespace UWPDVMod.App;

/// <summary>Turns a <see cref="SettingDef"/> into a WinForms control bound to the ini string value.</summary>
public static class SettingControlFactory
{
    /// <summary>A live control plus read/write delegates against its ini string value.</summary>
    public sealed class Bound
    {
        public required SettingDef Def { get; init; }
        public required Control Editor { get; init; }
        public required Func<string> GetValue { get; init; }
        public required Action<string> SetValue { get; init; }
        /// <summary>Non-null for Bool settings; used to drive dependent controls' enabled state.</summary>
        public CheckBox? BoolBox { get; init; }
    }

    public static Bound Build(SettingDef d) => d.Kind switch
    {
        ControlKind.Bool => BuildBool(d),
        ControlKind.Int => BuildNumeric(d, decimals: 0),
        ControlKind.Float => BuildNumeric(d, decimals: d.Decimals),
        ControlKind.Enum => BuildEnum(d, editable: false),
        ControlKind.EnumEditable => BuildEnum(d, editable: true),
        _ => throw new ArgumentOutOfRangeException(nameof(d)),
    };

    private static Bound BuildBool(SettingDef d)
    {
        var cb = new CheckBox { Text = d.Label, AutoSize = true };
        return new Bound
        {
            Def = d,
            Editor = cb,
            BoolBox = cb,
            GetValue = () => cb.Checked ? "true" : "false",
            SetValue = v => cb.Checked = v.Equals("true", StringComparison.OrdinalIgnoreCase),
        };
    }

    private static Bound BuildNumeric(SettingDef d, int decimals)
    {
        var n = new NumericUpDown
        {
            Minimum = d.Min,
            Maximum = d.Max,
            Increment = d.Step,
            DecimalPlaces = decimals,
            Width = decimals > 0 ? 80 : 90,
        };
        return new Bound
        {
            Def = d,
            Editor = n,
            GetValue = () => decimals > 0
                ? n.Value.ToString("0.####", CultureInfo.InvariantCulture)
                : ((long)n.Value).ToString(CultureInfo.InvariantCulture),
            SetValue = v =>
            {
                if (decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dv))
                    n.Value = Math.Clamp(dv, n.Minimum, n.Maximum);
            },
        };
    }

    private static Bound BuildEnum(SettingDef d, bool editable)
    {
        var cmb = new ComboBox
        {
            DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
            Width = 180,
        };
        foreach (var o in d.Options) cmb.Items.Add(o.Label);

        int IndexOfValue(string v) =>
            Enumerable.Range(0, d.Options.Count).FirstOrDefault(i => ValuesEqual(d.Options[i].Value, v), -1);

        return new Bound
        {
            Def = d,
            Editor = cmb,
            GetValue = () =>
            {
                // a picked/known label maps back to its stored value; free text is returned as-is
                int i = d.Options.ToList().FindIndex(o => o.Label.Equals(cmb.Text, StringComparison.OrdinalIgnoreCase));
                if (i >= 0) return d.Options[i].Value;
                return cmb.Text.Trim().Replace(',', '.');
            },
            SetValue = v =>
            {
                int i = IndexOfValue(v);
                if (i >= 0) cmb.SelectedIndex = i;
                else if (editable) cmb.Text = v;
                else if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            },
        };
    }

    private static bool ValuesEqual(string a, string b)
    {
        if (a.Equals(b, StringComparison.OrdinalIgnoreCase)) return true;
        return float.TryParse(a, NumberStyles.Any, CultureInfo.InvariantCulture, out float fa)
            && float.TryParse(b, NumberStyles.Any, CultureInfo.InvariantCulture, out float fb)
            && Math.Abs(fa - fb) < 0.0001f;
    }
}
