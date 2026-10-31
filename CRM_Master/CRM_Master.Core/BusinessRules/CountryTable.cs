namespace CRM_Master.Core.BusinessRules;

/// <summary>A country with its ISO 3166-1 alpha-2 code and international calling code.</summary>
public sealed record CountryInfo(string Iso2, string Name, string CallingCode);

/// <summary>Reference data for country-name / ISO-code validation and calling-code detection.</summary>
public static class CountryTable
{
    // ISO2|Name|CallingCode - covers India's major trade partners and all large markets.
    private const string Data =
        "IN|India|91;US|United States|1;CA|Canada|1;GB|United Kingdom|44;AU|Australia|61;NZ|New Zealand|64;SG|Singapore|65;AE|United Arab Emirates|971;" +
        "SA|Saudi Arabia|966;QA|Qatar|974;KW|Kuwait|965;OM|Oman|968;BH|Bahrain|973;PK|Pakistan|92;BD|Bangladesh|880;LK|Sri Lanka|94;NP|Nepal|977;" +
        "BT|Bhutan|975;MV|Maldives|960;AF|Afghanistan|93;CN|China|86;JP|Japan|81;KR|South Korea|82;HK|Hong Kong|852;TW|Taiwan|886;TH|Thailand|66;" +
        "MY|Malaysia|60;ID|Indonesia|62;PH|Philippines|63;VN|Vietnam|84;MM|Myanmar|95;KH|Cambodia|855;DE|Germany|49;FR|France|33;IT|Italy|39;" +
        "ES|Spain|34;PT|Portugal|351;NL|Netherlands|31;BE|Belgium|32;CH|Switzerland|41;AT|Austria|43;SE|Sweden|46;NO|Norway|47;DK|Denmark|45;" +
        "FI|Finland|358;IE|Ireland|353;PL|Poland|48;CZ|Czech Republic|420;GR|Greece|30;TR|Turkey|90;RU|Russia|7;UA|Ukraine|380;IL|Israel|972;" +
        "EG|Egypt|20;ZA|South Africa|27;NG|Nigeria|234;KE|Kenya|254;GH|Ghana|233;ET|Ethiopia|251;TZ|Tanzania|255;UG|Uganda|256;MA|Morocco|212;" +
        "BR|Brazil|55;AR|Argentina|54;MX|Mexico|52;CL|Chile|56;CO|Colombia|57;PE|Peru|51;MU|Mauritius|230;FJ|Fiji|679;IR|Iran|98;IQ|Iraq|964;" +
        "JO|Jordan|962;LB|Lebanon|961;HU|Hungary|36;RO|Romania|40;BG|Bulgaria|359;HR|Croatia|385;RS|Serbia|381;SK|Slovakia|421;LU|Luxembourg|352;" +
        "IS|Iceland|354;CY|Cyprus|357;MT|Malta|356;EE|Estonia|372;LV|Latvia|371;LT|Lithuania|370;KZ|Kazakhstan|7;UZ|Uzbekistan|998;DZ|Algeria|213;" +
        "TN|Tunisia|216;LY|Libya|218;SD|Sudan|249;ZW|Zimbabwe|263;ZM|Zambia|260;BW|Botswana|267;NA|Namibia|264;MZ|Mozambique|258;AO|Angola|244;" +
        "CM|Cameroon|237;SN|Senegal|221;CI|Ivory Coast|225;VE|Venezuela|58;EC|Ecuador|593;UY|Uruguay|598;PY|Paraguay|595;BO|Bolivia|591;" +
        "CR|Costa Rica|506;PA|Panama|507;DO|Dominican Republic|1;JM|Jamaica|1;TT|Trinidad and Tobago|1;PR|Puerto Rico|1;BN|Brunei|673;LA|Laos|856;MN|Mongolia|976";

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USA"] = "US", ["U.S.A."] = "US", ["U.S."] = "US", ["America"] = "US", ["United States of America"] = "US",
        ["UK"] = "GB", ["U.K."] = "GB", ["Great Britain"] = "GB", ["England"] = "GB", ["Scotland"] = "GB", ["Wales"] = "GB",
        ["UAE"] = "AE", ["Bharat"] = "IN", ["IND"] = "IN", ["Republic of India"] = "IN", ["Korea"] = "KR", ["Republic of Korea"] = "KR",
        ["Czechia"] = "CZ", ["Holland"] = "NL", ["Russian Federation"] = "RU", ["Viet Nam"] = "VN", ["Burma"] = "MM",
    };

    public static IReadOnlyList<CountryInfo> All { get; } = Data.Split(';')
        .Select(p => p.Split('|')).Select(p => new CountryInfo(p[0], p[1], p[2])).ToList();

    private static readonly Dictionary<string, CountryInfo> ByIso = All.ToDictionary(c => c.Iso2, StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CountryInfo> ByName = All.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CountryInfo> ByCallingCode = All
        .GroupBy(c => c.CallingCode).ToDictionary(g => g.Key, g => g.First());

    /// <summary>Resolves an ISO-2 code, English name or common alias; null when unrecognised.</summary>
    public static CountryInfo? Find(string? input)
    {
        var t = TextCleaner.Clean(input);
        if (t is null) return null;
        if (ByIso.TryGetValue(t, out var c)) return c;
        if (ByName.TryGetValue(t, out c)) return c;
        return Aliases.TryGetValue(t, out var iso) ? ByIso[iso] : null;
    }

    /// <summary>Finds the country for an international calling code ("91" -> India).</summary>
    public static CountryInfo? FromCallingCode(string code) => ByCallingCode.GetValueOrDefault(code);

    public static bool IsKnownCallingCode(string code) => ByCallingCode.ContainsKey(code);
}
