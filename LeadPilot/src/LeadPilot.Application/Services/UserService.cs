using System.Security.Cryptography;
using LeadPilot.Application.Abstractions;
using LeadPilot.Domain;

namespace LeadPilot.Application.Services;

/// <summary>Local users, optional PINs (PBKDF2-SHA256) and role permissions. This is app-level access control, not OS-level security.</summary>
public sealed class UserService : ICurrentUser
{
    private const int Iterations = 210_000;
    private readonly ISystemRepository _sys;
    private readonly IClock _clock;
    private SalesUser? _user;

    public UserService(ISystemRepository sys, IClock clock) { _sys = sys; _clock = clock; }

    public SalesUser? User => _user;
    public int? UserId => _user?.Id;
    public event EventHandler? Changed;

    public IReadOnlyList<SalesUser> Users() => _sys.GetUsers().Where(u => u.IsActive).ToList();

    /// <summary>Creates the first administrator during setup (or any user later).</summary>
    public SalesUser Create(string name, string? email, UserRole role, string? pin)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Enter a name.");
        var u = new SalesUser { Name = name.Trim(), Email = email, Role = role };
        SetPin(u, pin);
        _sys.AddUser(u);
        _sys.AddAudit(new AuditEntry { At = _clock.UtcNow, UserId = _user?.Id, Action = "user.create", Entity = "SalesUser", EntityId = u.Id.ToString(), Details = $"{u.Name} ({role})" });
        return u;
    }

    public void ChangePin(SalesUser u, string? pin) { SetPin(u, pin); _sys.UpdateUser(u); }

    /// <summary>Signs in. Users without a PIN sign in directly; otherwise the PIN must match (constant-time compare).</summary>
    public bool SignIn(SalesUser u, string? pin)
    {
        if (!u.IsActive) return false;
        if (u.PinHash != null)
        {
            if (string.IsNullOrEmpty(pin) || u.PinSalt == null) return Fail(u);
            var h = Hash(pin, Convert.FromBase64String(u.PinSalt));
            if (!CryptographicOperations.FixedTimeEquals(h, Convert.FromBase64String(u.PinHash))) return Fail(u);
        }
        _user = u;
        _sys.AddAudit(new AuditEntry { At = _clock.UtcNow, UserId = u.Id, Action = "signin", Entity = "SalesUser", EntityId = u.Id.ToString() });
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void SignOut() { _user = null; Changed?.Invoke(this, EventArgs.Empty); }

    private bool Fail(SalesUser u)
    {
        _sys.AddAudit(new AuditEntry { At = _clock.UtcNow, UserId = u.Id, Action = "signin.failed", Entity = "SalesUser", EntityId = u.Id.ToString() });
        return false;
    }

    private static void SetPin(SalesUser u, string? pin)
    {
        if (string.IsNullOrEmpty(pin)) { u.PinHash = null; u.PinSalt = null; return; }
        if (pin.Length < 4) throw new ArgumentException("A PIN must be at least 4 characters.");
        var salt = RandomNumberGenerator.GetBytes(16);
        u.PinSalt = Convert.ToBase64String(salt); u.PinHash = Convert.ToBase64String(Hash(pin, salt));
    }

    private static byte[] Hash(string pin, byte[] salt) => Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, 32);

    /// <summary>Role matrix: Sales works leads; Manager also exports and runs discovery; Admin manages everything.</summary>
    public bool Can(Permission p) => _user != null && (_user.Role switch
    {
        UserRole.Admin => true,
        UserRole.Manager => p is not (Permission.ManageUsers or Permission.ManageSettings or Permission.ManageBackups or Permission.ManageIntegrations),
        _ => p is Permission.ViewLeads or Permission.EditLeads or Permission.RunDiscovery
    });
}
