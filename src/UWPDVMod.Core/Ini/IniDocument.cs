using System.Text;
using System.Text.RegularExpressions;

namespace UWPDVMod.Core;

/// <summary>
/// Edits "Key = Value" lines in place within their [Section], leaving every other
/// line (comments, blank lines, ordering) untouched. Saving an unmodified document
/// reproduces the input byte-for-byte (newline style and trailing newline preserved).
/// </summary>
public class IniDocument
{
    private List<string> _lines = new();
    private string _newline = "\r\n";
    private bool _trailingNewline;
    private static readonly Regex SectionRx = new(@"^\s*\[(?<name>[^\]]+)\]\s*$", RegexOptions.Compiled);
    private static readonly Regex KeyRx = new(@"^(?<pre>\s*)(?<key>[A-Za-z0-9_. ]+?)(?<mid>\s*=\s*)(?<val>[^;]*?)(?<post>\s*(;.*)?)$", RegexOptions.Compiled);

    public string Path { get; }

    public IniDocument(string path)
    {
        Path = path;
        Reload();
    }

    public void Reload()
    {
        // preserve the file's framing exactly: newline style and trailing-newline presence
        string text = File.ReadAllText(Path, Encoding.UTF8);
        _newline = text.Contains("\r\n") ? "\r\n" : "\n";
        _trailingNewline = text.EndsWith("\n");
        if (_trailingNewline) text = text[..^_newline.Length];
        _lines = text.Split(_newline).ToList();
    }

    public void Save()
    {
        // The mod's ini has no BOM; keep it that way.
        File.WriteAllText(Path, string.Join(_newline, _lines) + (_trailingNewline ? _newline : ""), new UTF8Encoding(false));
    }

    public string? Get(string section, string key)
    {
        int i = FindKeyLine(section, key);
        if (i < 0) return null;
        var m = KeyRx.Match(_lines[i]);
        return m.Groups["val"].Value.Trim();
    }

    public bool GetBool(string section, string key, bool fallback = false)
        => Get(section, key) is string s ? s.Equals("true", StringComparison.OrdinalIgnoreCase) : fallback;

    public int GetInt(string section, string key, int fallback = 0)
        => int.TryParse(Get(section, key), out int v) ? v : fallback;

    public float GetFloat(string section, string key, float fallback = 0f)
        => float.TryParse(Get(section, key), System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : fallback;

    public void Set(string section, string key, string value)
    {
        int i = FindKeyLine(section, key);
        if (i >= 0)
        {
            var m = KeyRx.Match(_lines[i]);
            _lines[i] = m.Groups["pre"].Value + m.Groups["key"].Value + m.Groups["mid"].Value + value;
        }
        else
        {
            int s = FindSectionLine(section);
            if (s < 0)
            {
                if (_lines.Count > 0 && _lines[^1].Trim().Length > 0) _lines.Add("");
                _lines.Add($"[{section}]");
                _lines.Add($"{key} = {value}");
            }
            else
            {
                // insert after the last non-blank line of the section
                int end = s + 1;
                int lastContent = s;
                while (end < _lines.Count && !SectionRx.IsMatch(_lines[end]))
                {
                    if (_lines[end].Trim().Length > 0) lastContent = end;
                    end++;
                }
                _lines.Insert(lastContent + 1, $"{key} = {value}");
            }
        }
    }

    public void Set(string section, string key, bool value) => Set(section, key, value ? "true" : "false");
    public void Set(string section, string key, int value) => Set(section, key, value.ToString());
    public void Set(string section, string key, float value)
        => Set(section, key, value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture));

    private int FindSectionLine(string section)
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            var m = SectionRx.Match(_lines[i]);
            if (m.Success && m.Groups["name"].Value.Trim().Equals(section, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private int FindKeyLine(string section, string key)
    {
        int s = FindSectionLine(section);
        if (s < 0) return -1;
        for (int i = s + 1; i < _lines.Count && !SectionRx.IsMatch(_lines[i]); i++)
        {
            string t = _lines[i].TrimStart();
            if (t.StartsWith(";") || t.StartsWith("#")) continue;
            var m = KeyRx.Match(_lines[i]);
            if (m.Success && m.Groups["key"].Value.Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }
}
