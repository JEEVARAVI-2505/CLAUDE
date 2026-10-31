using CRM_Master.Core.BusinessRules;

namespace CRM_Master.Engine.Import;

/// <summary>Decides, row by row, whether a row is new, a duplicate of existing data, a duplicate within the file, invalid or empty.</summary>
internal sealed class RowClassifier
{
    private readonly IReadOnlyDictionary<string, int> _existingEmail, _existingPhone;
    private readonly Dictionary<string, int> _fileEmail = new(), _filePhone = new();
    private readonly IReadOnlyList<ColumnMapping> _mapping;
    private readonly bool _clean;
    private readonly string _cc;

    public RowClassifier(IReadOnlyDictionary<string, int> existingEmail, IReadOnlyDictionary<string, int> existingPhone,
        IReadOnlyList<ColumnMapping> mapping, bool clean, string countryCode)
    { _existingEmail = existingEmail; _existingPhone = existingPhone; _mapping = mapping; _clean = clean; _cc = countryCode; }

    public (ConvertedRow Row, PreviewRowStatus Status, int? ExistingId, string Message) Classify(string?[] cells, int rowNumber)
    {
        var row = RowConverter.Convert(cells, _mapping, rowNumber, _clean, _cc);
        if (row.IsEmpty) return (row, PreviewRowStatus.Empty, null, "Empty row");
        if (!row.IsIdentifiable) return (row, PreviewRowStatus.Invalid, null, "No name, email or phone - nothing to identify this person");

        var c = row.Contact;
        ContactKeys.Apply(c, _cc);
        int? existing = null; string? why = null;
        if (c.EmailKey is not null && _existingEmail.TryGetValue(c.EmailKey, out var e1)) { existing = e1; why = "Email already exists"; }
        else if (c.PhoneKey is not null && _existingPhone.TryGetValue(c.PhoneKey, out var e2)) { existing = e2; why = "Phone already exists"; }
        else if (c.AlternatePhoneKey is not null && _existingPhone.TryGetValue(c.AlternatePhoneKey, out var e3)) { existing = e3; why = "Alternate phone already exists"; }
        if (existing is { } id) return (row, PreviewRowStatus.DuplicateExisting, id, $"{why} (contact #{id})");

        int? firstRow = null;
        if (c.EmailKey is not null && _fileEmail.TryGetValue(c.EmailKey, out var r1)) firstRow = r1;
        else if (c.PhoneKey is not null && _filePhone.TryGetValue(c.PhoneKey, out var r2)) firstRow = r2;
        if (firstRow is { } fr) return (row, PreviewRowStatus.DuplicateInFile, null, $"Same email/phone as row {fr} in this file");

        if (c.EmailKey is not null) _fileEmail[c.EmailKey] = rowNumber;
        if (c.PhoneKey is not null) _filePhone[c.PhoneKey] = rowNumber;
        if (c.AlternatePhoneKey is not null) _filePhone.TryAdd(c.AlternatePhoneKey, rowNumber);

        var warnings = new List<string>(row.Warnings);
        if (c.Email is not null && !EmailRules.Check(c.Email).IsValid) warnings.Add("Email looks invalid");
        if (c.Phone is not null && !PhoneNumberParser.Parse(c.Phone, _cc).IsValid) warnings.Add("Phone looks invalid");
        return warnings.Count == 0 ? (row, PreviewRowStatus.Ready, null, "") : (row, PreviewRowStatus.Warning, null, string.Join("; ", warnings));
    }
}
