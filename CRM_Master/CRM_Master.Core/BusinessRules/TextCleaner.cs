using System.Text;
using System.Text.RegularExpressions;

namespace CRM_Master.Core.BusinessRules;

/// <summary>Whitespace and control-character hygiene shared by all cleaners.</summary>
public static partial class TextCleaner
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    /// <summary>Trims, collapses internal whitespace (incl. non-breaking spaces) and removes control / zero-width characters. Returns null for empty input.</summary>
    public static string? Clean(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch is '​' or '‌' or '‍' or '﻿') continue;           // zero-width
            if (ch is ' ' or '\t' or '\r' or '\n') { sb.Append(' '); continue; }   // whitespace variants
            if (char.IsControl(ch)) continue;
            sb.Append(ch);
        }
        var cleaned = Spaces().Replace(sb.ToString(), " ").Trim();
        return cleaned.Length == 0 ? null : cleaned;
    }

    /// <summary>Like <see cref="Clean"/> but keeps line breaks (for notes / addresses).</summary>
    public static string? CleanMultiline(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        var lines = value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
            .Select(l => Clean(l)).Where(l => l is not null);
        var joined = string.Join("\n", lines!);
        return joined.Length == 0 ? null : joined;
    }

    public static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);
}
