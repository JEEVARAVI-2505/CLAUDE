using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.Import;

/// <summary>Result of turning one spreadsheet row into a contact.</summary>
/// <param name="IsEmpty">All mapped cells were blank.</param>
/// <param name="IsIdentifiable">False when the row has no name, email or phone - nothing identifies the person.</param>
public sealed record ConvertedRow(int RowNumber, Contact Contact, Dictionary<string, string?> Custom, List<string> Warnings, bool IsEmpty, bool IsIdentifiable);

/// <summary>Maps raw cells to a normalised <see cref="Contact"/>. Pure and deterministic.</summary>
public static class RowConverter
{
    public static ConvertedRow Convert(string?[] cells, IReadOnlyList<ColumnMapping> mapping, int rowNumber, bool clean, string countryCode)
    {
        var c = new Contact();
        var custom = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();
        var any = false;

        foreach (var m in mapping)
        {
            if (m.IsIgnored || m.SourceIndex >= cells.Length) continue;
            var raw = cells[m.SourceIndex];
            if (TextCleaner.IsBlank(raw)) continue;
            any = true;

            if (m.CustomFieldName is { } cf) { custom[cf.Trim()] = TextCleaner.Clean(raw); continue; }
            var field = m.Target!.Value;
            var value = field is ContactField.Address or ContactField.Notes ? TextCleaner.CleanMultiline(raw) : TextCleaner.Clean(raw);
            if (clean) value = Normalise(field, value, countryCode, warnings);
            if (!c.Set(field, value)) warnings.Add($"Invalid date '{value}' in {Contact.LabelOf(field)} was ignored");
        }

        if (TextCleaner.IsBlank(c.FullName) && (c.FirstName is not null || c.LastName is not null))
            c.FullName = $"{c.FirstName} {c.LastName}".Trim();

        var identifiable = ContactQuality.HasName(c) || c.Email is not null || c.Phone is not null;
        return new ConvertedRow(rowNumber, c, custom, warnings, !any, identifiable);
    }

    private static string? Normalise(ContactField f, string? v, string cc, List<string> warnings)
    {
        if (v is null) return null;
        switch (f)
        {
            case ContactField.FirstName: case ContactField.LastName: case ContactField.FullName: return NameCaser.Proper(v);
            case ContactField.Email: return EmailRules.Normalize(v);
            case ContactField.Phone: case ContactField.AlternatePhone:
                var p = PhoneNumberParser.Parse(v, cc);
                return p.IsValid ? p.Formatted + (p.Extension is null ? "" : " x" + p.Extension) : v;
            case ContactField.Website: return UrlRules.Normalize(v) ?? v;
            default: return v;
        }
    }
}
