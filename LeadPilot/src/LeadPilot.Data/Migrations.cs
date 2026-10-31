namespace LeadPilot.Data;

/// <summary>Ordered, append-only schema migrations. Never edit a released migration; add a new one.</summary>
public static class Migrations
{
    /// <summary>All migrations.</summary>
    public static readonly (int Version, string Sql)[] All =
    {
        (1, @"
CREATE TABLE Businesses(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Name TEXT NOT NULL CHECK(length(trim(Name)) > 0),
  Website TEXT, Domain TEXT, Phone TEXT, NormalizedPhone TEXT, Email TEXT, NormalizedEmail TEXT,
  Address TEXT, City TEXT, Region TEXT, Country TEXT, PostalCode TEXT,
  Industry TEXT, Services TEXT, Description TEXT, ContactPageUrl TEXT, CompanySize TEXT,
  Latitude REAL, Longitude REAL,
  CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL);
CREATE INDEX IX_Businesses_Name ON Businesses(Name);
CREATE INDEX IX_Businesses_Domain ON Businesses(Domain);
CREATE INDEX IX_Businesses_Phone ON Businesses(NormalizedPhone);
CREATE INDEX IX_Businesses_Email ON Businesses(NormalizedEmail);
CREATE INDEX IX_Businesses_City ON Businesses(City);

CREATE TABLE SalesUsers(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Name TEXT NOT NULL, Email TEXT,
  Role INTEGER NOT NULL CHECK(Role BETWEEN 0 AND 2),
  PinHash TEXT, PinSalt TEXT, IsActive INTEGER NOT NULL DEFAULT 1, CreatedAt TEXT NOT NULL);

CREATE TABLE Campaigns(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Name TEXT NOT NULL, Product TEXT NOT NULL DEFAULT '', TargetCustomer TEXT NOT NULL DEFAULT '', Location TEXT NOT NULL DEFAULT '',
  TargetCount INTEGER NOT NULL DEFAULT 0, Status INTEGER NOT NULL DEFAULT 0 CHECK(Status BETWEEN 0 AND 3), Notes TEXT, CreatedAt TEXT NOT NULL);

CREATE TABLE Contacts(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  BusinessId INTEGER NOT NULL REFERENCES Businesses(Id) ON DELETE CASCADE,
  FullName TEXT NOT NULL, JobTitle TEXT, Phone TEXT, NormalizedPhone TEXT, Email TEXT, NormalizedEmail TEXT,
  IsPrimary INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL);
CREATE INDEX IX_Contacts_Business ON Contacts(BusinessId);
CREATE INDEX IX_Contacts_Email ON Contacts(NormalizedEmail);

CREATE TABLE Leads(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  LeadCode TEXT NOT NULL UNIQUE,
  BusinessId INTEGER NOT NULL REFERENCES Businesses(Id) ON DELETE CASCADE,
  PrimaryContactId INTEGER REFERENCES Contacts(Id) ON DELETE SET NULL,
  CampaignId INTEGER REFERENCES Campaigns(Id) ON DELETE SET NULL,
  AssignedUserId INTEGER REFERENCES SalesUsers(Id) ON DELETE SET NULL,
  Status INTEGER NOT NULL DEFAULT 0 CHECK(Status BETWEEN 0 AND 6),
  Score INTEGER NOT NULL DEFAULT 0 CHECK(Score BETWEEN 0 AND 100),
  Temperature INTEGER NOT NULL DEFAULT 0 CHECK(Temperature BETWEEN 0 AND 3),
  ScoreExplanation TEXT,
  EmailStatus INTEGER NOT NULL DEFAULT 0, PhoneStatus INTEGER NOT NULL DEFAULT 0, WebsiteStatus INTEGER NOT NULL DEFAULT 0,
  IntentEvidence TEXT,
  IsArchived INTEGER NOT NULL DEFAULT 0, MergedIntoLeadId INTEGER,
  SourceName TEXT NOT NULL DEFAULT 'Manual',
  CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL, LastContactedAt TEXT, NextFollowUpAt TEXT);
CREATE INDEX IX_Leads_Business ON Leads(BusinessId);
CREATE INDEX IX_Leads_Status ON Leads(Status);
CREATE INDEX IX_Leads_Score ON Leads(Score);
CREATE INDEX IX_Leads_Source ON Leads(SourceName);
CREATE INDEX IX_Leads_Campaign ON Leads(CampaignId);
CREATE INDEX IX_Leads_Assigned ON Leads(AssignedUserId);
CREATE INDEX IX_Leads_Created ON Leads(CreatedAt);
CREATE INDEX IX_Leads_NextFollowUp ON Leads(NextFollowUpAt);

CREATE TABLE LeadSources(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  LeadId INTEGER NOT NULL REFERENCES Leads(Id) ON DELETE CASCADE,
  SourceName TEXT NOT NULL, SourceType TEXT NOT NULL, SourceUrl TEXT, ExternalId TEXT, License TEXT, RetrievedAt TEXT NOT NULL);
CREATE INDEX IX_LeadSources_Lead ON LeadSources(LeadId);

CREATE TABLE LeadFieldsEvidence(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  LeadId INTEGER NOT NULL REFERENCES Leads(Id) ON DELETE CASCADE,
  FieldName TEXT NOT NULL, Value TEXT NOT NULL, SourceName TEXT NOT NULL, SourceUrl TEXT,
  ExtractedAt TEXT NOT NULL, Confidence REAL NOT NULL DEFAULT 0, Applied INTEGER NOT NULL DEFAULT 0, Note TEXT);
CREATE INDEX IX_Evidence_Lead ON LeadFieldsEvidence(LeadId);

CREATE TABLE DuplicateMatches(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  LeadAId INTEGER NOT NULL REFERENCES Leads(Id) ON DELETE CASCADE,
  LeadBId INTEGER NOT NULL REFERENCES Leads(Id) ON DELETE CASCADE,
  Score REAL NOT NULL, Confidence INTEGER NOT NULL CHECK(Confidence BETWEEN 0 AND 2), Reasons TEXT NOT NULL,
  Decision INTEGER NOT NULL DEFAULT 0 CHECK(Decision BETWEEN 0 AND 2),
  CreatedAt TEXT NOT NULL, DecidedAt TEXT, DecidedBy TEXT,
  UNIQUE(LeadAId, LeadBId), CHECK(LeadAId < LeadBId));
CREATE INDEX IX_Dup_Decision ON DuplicateMatches(Decision);

CREATE TABLE MergeHistory(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  SurvivorLeadId INTEGER NOT NULL REFERENCES Leads(Id) ON DELETE CASCADE,
  MergedLeadId INTEGER NOT NULL REFERENCES Leads(Id) ON DELETE CASCADE,
  MergedAt TEXT NOT NULL, MergedBy TEXT, UndoneAt TEXT,
  SurvivorBeforeJson TEXT NOT NULL, MergedBeforeJson TEXT NOT NULL, MovedJson TEXT NOT NULL);

CREATE TABLE LeadActivities(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  LeadId INTEGER NOT NULL REFERENCES Leads(Id) ON DELETE CASCADE,
  Type INTEGER NOT NULL, Subject TEXT NOT NULL, Body TEXT, CreatedAt TEXT NOT NULL,
  UserId INTEGER REFERENCES SalesUsers(Id) ON DELETE SET NULL);
CREATE INDEX IX_Activities_Lead ON LeadActivities(LeadId, CreatedAt);

CREATE TABLE FollowUpTasks(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  LeadId INTEGER NOT NULL REFERENCES Leads(Id) ON DELETE CASCADE,
  Title TEXT NOT NULL, Notes TEXT, DueAt TEXT NOT NULL,
  Status INTEGER NOT NULL DEFAULT 0 CHECK(Status BETWEEN 0 AND 2),
  AssignedUserId INTEGER REFERENCES SalesUsers(Id) ON DELETE SET NULL,
  CreatedAt TEXT NOT NULL, CompletedAt TEXT);
CREATE INDEX IX_Tasks_Due ON FollowUpTasks(Status, DueAt);
CREATE INDEX IX_Tasks_Lead ON FollowUpTasks(LeadId);

CREATE TABLE Settings(Key TEXT PRIMARY KEY, Value TEXT NOT NULL, UpdatedAt TEXT NOT NULL);

CREATE TABLE ImportHistory(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  FileName TEXT NOT NULL, FileHash TEXT, TotalRows INTEGER NOT NULL, Imported INTEGER NOT NULL, Duplicates INTEGER NOT NULL,
  Skipped INTEGER NOT NULL, Errors INTEGER NOT NULL, ReportJson TEXT NOT NULL, ImportedAt TEXT NOT NULL, UserId INTEGER);

CREATE TABLE AuditLogs(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  At TEXT NOT NULL, UserId INTEGER, Action TEXT NOT NULL, Entity TEXT, EntityId TEXT, Details TEXT);
CREATE INDEX IX_Audit_At ON AuditLogs(At);

CREATE TABLE ApiUsage(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  At TEXT NOT NULL, Provider TEXT NOT NULL, Operation TEXT NOT NULL, Requests INTEGER NOT NULL DEFAULT 1,
  Success INTEGER NOT NULL, ResultCount INTEGER NOT NULL DEFAULT 0, ErrorMessage TEXT);
CREATE INDEX IX_ApiUsage_Provider ON ApiUsage(Provider, At);
")
    };
}
