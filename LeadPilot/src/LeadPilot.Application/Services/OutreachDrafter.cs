using System.Net;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Domain;

namespace LeadPilot.Application.Services;

public enum OutreachChannel { Email, WhatsApp }

/// <summary>What the user writes about themselves; used to personalise drafts.</summary>
public sealed class SenderProfile
{
    public string SenderName { get; set; } = "";
    public string SenderCompany { get; set; } = "";
    public string Offer { get; set; } = "";
}

/// <summary>A draft message. Nothing is sent by this application; the user sends it from their own mail/WhatsApp.</summary>
public sealed record OutreachDraft(OutreachChannel Channel, string Subject, string Body, string? RecipientEmail, string? RecipientPhone, bool UsedAi);

/// <summary>Creates personalised draft messages from stored facts. Optional AI polishing; always falls back to the template.</summary>
public sealed class OutreachDrafter
{
    private readonly IAiClient _ai;
    private readonly IWorkRepository _work;

    public OutreachDrafter(IAiClient ai, IWorkRepository work) { _ai = ai; _work = work; }

    /// <summary>Template-based draft (deterministic).</summary>
    public static OutreachDraft FromTemplate(LeadDetail d, SenderProfile me, OutreachChannel channel)
    {
        var contact = d.Contacts.FirstOrDefault(c => c.Id == d.Lead.PrimaryContactId);
        var greeting = contact != null ? $"Hello {contact.FullName.Split(' ')[0]}" : $"Hello {d.Business.Name} team";
        var offer = string.IsNullOrWhiteSpace(me.Offer) ? "our services" : me.Offer.Trim();
        var where = string.IsNullOrWhiteSpace(d.Business.City) ? "" : $" in {d.Business.City}";
        var sector = string.IsNullOrWhiteSpace(d.Business.Industry) ? "" : $" {d.Business.Industry!.ToLowerInvariant()}";
        var sign = string.IsNullOrWhiteSpace(me.SenderCompany) ? me.SenderName : $"{me.SenderName}, {me.SenderCompany}";
        var email = contact?.Email ?? d.Business.Email; var phone = contact?.Phone ?? d.Business.Phone;

        if (channel == OutreachChannel.WhatsApp)
        {
            var text = $"{greeting}, this is {sign}. We help{sector} businesses{where} with {offer}. Would it be OK if I share a few details? Reply STOP and I won't message again.";
            return new OutreachDraft(channel, "", text, email, phone, false);
        }
        var subject = $"{Truncate(offer, 50)} for {d.Business.Name}";
        var body = $"{greeting},\n\nI'm {me.SenderName}{(string.IsNullOrWhiteSpace(me.SenderCompany) ? "" : " from " + me.SenderCompany)}. I'm getting in touch because {d.Business.Name} looks like a{sector} business{where} that could benefit from {offer}.\n\n" +
                   "Would you be open to a short call this week to see whether it's a fit?\n\nBest regards,\n" + sign + "\n\n" +
                   "If you'd rather not hear from me, just reply \"stop\" and I won't contact you again.";
        return new OutreachDraft(channel, subject, body, email, phone, false);
    }

    /// <summary>Creates a draft, optionally polished by AI, and saves it on the lead's timeline.</summary>
    public async Task<OutreachDraft> DraftAsync(LeadDetail d, SenderProfile me, OutreachChannel channel, bool useAi, CancellationToken ct = default)
    {
        var draft = FromTemplate(d, me, channel);
        if (useAi && _ai.IsAvailable)
        {
            try
            {
                var polished = await _ai.CompleteAsync(
                    "You edit sales messages. Keep every fact exactly as given, do not add claims, prices, statistics or promises. Keep the opt-out sentence. Return only the message text.",
                    $"Rewrite this {channel} message to sound natural and concise:\n\n{draft.Body}", ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(polished) && polished.Length > 40) draft = draft with { Body = polished.Trim(), UsedAi = true };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException) { /* fall back to template */ }
        }
        _work.AddActivity(new LeadActivity { LeadId = d.Lead.Id, Type = ActivityType.Draft, Subject = $"{channel} draft prepared", Body = (draft.Subject.Length > 0 ? draft.Subject + "\n\n" : "") + draft.Body });
        return draft;
    }

    /// <summary>mailto: link that opens the user's own mail client.</summary>
    public static string MailTo(string? to, string subject, string body) =>
        $"mailto:{WebUtility.UrlEncode(to ?? "").Replace("%40", "@")}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

    /// <summary>WhatsApp click-to-chat link (the user presses Send in WhatsApp).</summary>
    public static string? WhatsAppLink(string? e164, string text)
    {
        var digits = new string((e164 ?? "").Where(char.IsDigit).ToArray());
        return digits.Length < 7 ? null : $"https://wa.me/{digits}?text={Uri.EscapeDataString(text)}";
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n].TrimEnd() + "…";
}
