using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using DiegoG.ToolSite.Server.Types;
using DiegoG.ToolSite.Shared.Types;

namespace DiegoG.ToolSite.Server.Database.Models.Ledger;

public class LedgerEntry : MutableDbModel, IKeyed<LedgerEntry>
{
    public required Id<LedgerEntry> Id { get; init; }

    public required Id<User> UserId { get; init; }
    public User User { get; init; }

    [StringLength(3, MinimumLength = 3)]
    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public string CurrencyCode
    {
        get => Money.Currency.Code;
        set => Money = new(CurrencyAmount, value);
    }

    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public decimal CurrencyAmount
    {
        get => Money.Amount;
        set => Money = new(value, Money.Currency.Code ?? "USD");
    }

    public required DateTimeOffset Date { get; set; }

    [NotMapped]
    public Money Money { get; set; }

    public string? Message { get; set; }
    
    public string? Category { get; set; }
    
    public string? Recipient { get; set; }

    public HashSet<Tag<LedgerEntry>> Tags { get; } = new HashSet<Tag<LedgerEntry>>(
        new DelegateEqualityComparer<Tag<LedgerEntry>>(
            (x, y) => string.Equals(x?.Label, y?.Label, StringComparison.CurrentCultureIgnoreCase),
            x => string.GetHashCode(x?.Label, StringComparison.CurrentCultureIgnoreCase)
        )
    );
}
