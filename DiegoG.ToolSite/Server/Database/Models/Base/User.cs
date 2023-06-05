using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DiegoG.ToolSite.Server.Database.Models.Base;

public class User : MutableDbModel, IKeyed<User>
{
    public Id<User> Id { get; init; }

    public required string Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? Email { get; set; }

    private MailAddress? _em;
    [IgnoreDataMember, JsonIgnore, XmlIgnore, NotMapped]
    public MailAddress? EmailAddress
    {
        get => Email is not null ? _em ??= new(Email) : null;
        set => Email = (_em = value)?.Address;
    }

    public bool IsMailConfirmed { get; set; }
    public MailConfirmationRequest? MailConfirmationRequest { get; set; }
    public HashSet<Role> Roles { get; } = new();

    private string? _visname;
    public string DisplayName => _visname ??= $"{(PasswordHash is null ? "(Anonymous) " : "")}{Username}";

    public UserSettings? UserSettings { get; init; }

    public HashSet<LedgerEntry> LedgerEntries { get; } = new();
}
