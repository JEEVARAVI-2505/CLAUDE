using LeadPilot.Domain;
using Microsoft.Data.Sqlite;

namespace LeadPilot.Data;

/// <summary>Column lists and row mappers. Column order in each constant must match its mapper.</summary>
internal static class Mappers
{
    public const string BusinessCols = "Id,Name,Website,Domain,Phone,NormalizedPhone,Email,NormalizedEmail,Address,City,Region,Country,PostalCode,Industry,Services,Description,ContactPageUrl,CompanySize,Latitude,Longitude,CreatedAt,UpdatedAt";
    public static Business Business(SqliteDataReader r) => new()
    {
        Id = r.I(0), Name = r.S(1), Website = r.NS(2), Domain = r.NS(3), Phone = r.NS(4), NormalizedPhone = r.NS(5), Email = r.NS(6), NormalizedEmail = r.NS(7),
        Address = r.NS(8), City = r.NS(9), Region = r.NS(10), Country = r.NS(11), PostalCode = r.NS(12), Industry = r.NS(13), Services = r.NS(14), Description = r.NS(15),
        ContactPageUrl = r.NS(16), CompanySize = r.NS(17), Latitude = r.ND(18), Longitude = r.ND(19), CreatedAt = r.Dt(20), UpdatedAt = r.Dt(21)
    };

    public static (string Name, object? Value)[] BusinessParams(Business b) => new (string, object?)[]
    {
        ("@name", b.Name), ("@web", b.Website), ("@dom", b.Domain), ("@ph", b.Phone), ("@nph", b.NormalizedPhone), ("@em", b.Email), ("@nem", b.NormalizedEmail),
        ("@addr", b.Address), ("@city", b.City), ("@reg", b.Region), ("@ctry", b.Country), ("@pc", b.PostalCode), ("@ind", b.Industry), ("@svc", b.Services),
        ("@desc", b.Description), ("@cp", b.ContactPageUrl), ("@size", b.CompanySize), ("@lat", b.Latitude), ("@lon", b.Longitude), ("@cr", b.CreatedAt), ("@up", b.UpdatedAt)
    };

    public const string ContactCols = "Id,BusinessId,FullName,JobTitle,Phone,NormalizedPhone,Email,NormalizedEmail,IsPrimary,CreatedAt,UpdatedAt";
    public static Contact Contact(SqliteDataReader r) => new()
    {
        Id = r.I(0), BusinessId = r.I(1), FullName = r.S(2), JobTitle = r.NS(3), Phone = r.NS(4), NormalizedPhone = r.NS(5), Email = r.NS(6), NormalizedEmail = r.NS(7),
        IsPrimary = r.B(8), CreatedAt = r.Dt(9), UpdatedAt = r.Dt(10)
    };

    public const string LeadCols = "Id,LeadCode,BusinessId,PrimaryContactId,CampaignId,AssignedUserId,Status,Score,Temperature,ScoreExplanation,EmailStatus,PhoneStatus,WebsiteStatus,IntentEvidence,IsArchived,MergedIntoLeadId,SourceName,CreatedAt,UpdatedAt,LastContactedAt,NextFollowUpAt";
    public static Lead Lead(SqliteDataReader r) => new()
    {
        Id = r.I(0), LeadCode = r.S(1), BusinessId = r.I(2), PrimaryContactId = r.NI(3), CampaignId = r.NI(4), AssignedUserId = r.NI(5),
        Status = r.E<LeadStatus>(6), Score = r.I(7), Temperature = r.E<LeadTemperature>(8), ScoreExplanation = r.NS(9),
        EmailStatus = r.E<EmailStatus>(10), PhoneStatus = r.E<PhoneStatus>(11), WebsiteStatus = r.E<WebsiteStatus>(12), IntentEvidence = r.NS(13),
        IsArchived = r.B(14), MergedIntoLeadId = r.NI(15), SourceName = r.S(16), CreatedAt = r.Dt(17), UpdatedAt = r.Dt(18), LastContactedAt = r.NDt(19), NextFollowUpAt = r.NDt(20)
    };

    public const string SourceCols = "Id,LeadId,SourceName,SourceType,SourceUrl,ExternalId,License,RetrievedAt";
    public static LeadSourceRecord Source(SqliteDataReader r) => new()
    {
        Id = r.I(0), LeadId = r.I(1), SourceName = r.S(2), SourceType = r.S(3), SourceUrl = r.NS(4), ExternalId = r.NS(5), License = r.NS(6), RetrievedAt = r.Dt(7)
    };

    public const string EvidenceCols = "Id,LeadId,FieldName,Value,SourceName,SourceUrl,ExtractedAt,Confidence,Applied,Note";
    public static FieldEvidence Evidence(SqliteDataReader r) => new()
    {
        Id = r.I(0), LeadId = r.I(1), FieldName = r.S(2), Value = r.S(3), SourceName = r.S(4), SourceUrl = r.NS(5), ExtractedAt = r.Dt(6), Confidence = r.D(7), Applied = r.B(8), Note = r.NS(9)
    };

    public const string ActivityCols = "Id,LeadId,Type,Subject,Body,CreatedAt,UserId";
    public static LeadActivity Activity(SqliteDataReader r) => new()
    {
        Id = r.I(0), LeadId = r.I(1), Type = r.E<ActivityType>(2), Subject = r.S(3), Body = r.NS(4), CreatedAt = r.Dt(5), UserId = r.NI(6)
    };

    public const string TaskCols = "Id,LeadId,Title,Notes,DueAt,Status,AssignedUserId,CreatedAt,CompletedAt";
    public static FollowUpTask Task(SqliteDataReader r) => new()
    {
        Id = r.I(0), LeadId = r.I(1), Title = r.S(2), Notes = r.NS(3), DueAt = r.Dt(4), Status = r.E<FollowUpStatus>(5), AssignedUserId = r.NI(6), CreatedAt = r.Dt(7), CompletedAt = r.NDt(8)
    };

    public const string CampaignCols = "Id,Name,Product,TargetCustomer,Location,TargetCount,Status,Notes,CreatedAt";
    public static Campaign Campaign(SqliteDataReader r) => new()
    {
        Id = r.I(0), Name = r.S(1), Product = r.S(2), TargetCustomer = r.S(3), Location = r.S(4), TargetCount = r.I(5), Status = r.E<CampaignStatus>(6), Notes = r.NS(7), CreatedAt = r.Dt(8)
    };

    public const string UserCols = "Id,Name,Email,Role,PinHash,PinSalt,IsActive,CreatedAt";
    public static SalesUser User(SqliteDataReader r) => new()
    {
        Id = r.I(0), Name = r.S(1), Email = r.NS(2), Role = r.E<UserRole>(3), PinHash = r.NS(4), PinSalt = r.NS(5), IsActive = r.B(6), CreatedAt = r.Dt(7)
    };
}
