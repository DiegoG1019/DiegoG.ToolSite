using DiegoG.ToolSite.Server.Database;

namespace DiegoG.ToolSite.Server.Services;

public class LedgerManager
{
    private readonly ToolSiteContext Db;

    public LedgerManager(ToolSiteContext db)
    {
        Db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public IQueryable<LedgerEntry> FetchItems(DateTimeOffset start, DateTimeOffset end, int page, int pageSize, Id<User> userId)
        => Db.LedgerEntries
            .Where(x => x.UserId == userId && x.Date >= start && x.Date <= end)
            .OrderByDescending(x => x.Date)
            .Skip(page * pageSize)
            .Take(pageSize);

    public IQueryable<LedgerEntry> FetchItems(IEnumerable<Id<LedgerEntry>> ids, Id<User> userId)
        => Db.LedgerEntries
            .Where(x => x.UserId == userId)
            .Join(ids, x => x.Id, y => y, (x, y) => x);

    public LedgerEntry AddItem(LedgerEntryItem item, Id<User> userId)
    {
        var entry = new LedgerEntry()
        {
            Category = item.Category,
            Money = item.Money?.ToNodaMoney() ?? throw new ArgumentException("Money property must not be null", nameof(item)),
            Date = item.Date ?? throw new ArgumentException("Date property must not be null", nameof(item)),
            Id = Id<LedgerEntry>.New(),
            UserId = userId,
            Message = item.Message,
            Recipient = item.Recipient
        };

        if (item.Tags is not null)
            foreach (var tag in item.Tags)
                entry.Tags.Add(new Tag<LedgerEntry>()
                {
                    OwnerId = entry.Id,
                    Label = tag
                });

        Db.LedgerEntries.Add(entry);
        return entry;
    }

    public void SaveChanges()
        => Db.SaveChanges();

    public Task SaveChangesAsync()
        => Db.SaveChangesAsync();
}
