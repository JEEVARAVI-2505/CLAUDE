namespace LeadPilot.Domain;

/// <summary>Sales pipeline stage of a lead.</summary>
public enum LeadStatus { New = 0, Contacted = 1, Qualified = 2, Proposal = 3, Won = 4, Lost = 5, Unqualified = 6 }

/// <summary>Relevance bucket derived from the 0-100 score. This is relevance, not purchasing intent.</summary>
public enum LeadTemperature { Unqualified = 0, Cold = 1, Warm = 2, Hot = 3 }

/// <summary>What is known about an email address. Never implies the mailbox belongs to a person.</summary>
public enum EmailStatus { Unverified = 0, InvalidFormat = 1, ValidFormat = 2, DomainExists = 3, MailServerFound = 4, VerifiedByProvider = 5 }

/// <summary>What is known about a phone number.</summary>
public enum PhoneStatus { Unverified = 0, InvalidFormat = 1, Normalized = 2, CountryIdentified = 3, PubliclyListed = 4, VerifiedByProvider = 5 }

/// <summary>Result of the last website check.</summary>
public enum WebsiteStatus { Unverified = 0, Accessible = 1, Unavailable = 2, Redirected = 3, DomainMismatch = 4 }

/// <summary>How sure the duplicate detector is.</summary>
public enum DuplicateConfidence { Low = 0, Medium = 1, High = 2 }

/// <summary>User decision on a candidate duplicate pair.</summary>
public enum DuplicateDecision { Pending = 0, Merged = 1, NotDuplicate = 2 }

/// <summary>State of a follow-up task.</summary>
public enum FollowUpStatus { Open = 0, Done = 1, Cancelled = 2 }

/// <summary>Kind of timeline entry.</summary>
public enum ActivityType { Note = 0, Call = 1, Email = 2, Meeting = 3, WhatsApp = 4, StatusChange = 5, Created = 6, Imported = 7, Merged = 8, Enriched = 9, Scored = 10, Draft = 11 }

/// <summary>Application-level role.</summary>
public enum UserRole { Sales = 0, Manager = 1, Admin = 2 }

/// <summary>Campaign life-cycle state.</summary>
public enum CampaignStatus { Draft = 0, Active = 1, Paused = 2, Completed = 3 }
