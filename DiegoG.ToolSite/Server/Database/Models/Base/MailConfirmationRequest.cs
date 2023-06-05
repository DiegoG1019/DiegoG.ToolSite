using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DiegoG.ToolSite.Server.Database.Models.Base;

public class MailConfirmationRequest : IDispatchable, IKeyed<MailConfirmationRequest>
{
    public required Id<MailConfirmationRequest> Id { get; init; }
    public required Id<User> UserId { get; init; }
    public required DateTimeOffset CreationDate { get; init; }
    public required string Email { get; init; }

    private MailAddress? _em;

    [IgnoreDataMember, JsonIgnore, XmlIgnore, NotMapped]
    public MailAddress EmailAddress => _em ??= new(Email);

    public User User { get; init; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public ServerInfo? ClaimedBy { get; set; }
    public DateTimeOffset? DispatchedAt { get; set; }
}
