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

    public void AddItem(LedgerEntryItem item, Id<User> userId)
    {
        var entry = new LedgerEntry()
        {
            Category = item.Category,
            Money = item.Money,
            Date = item.Date,
            Id = Id<LedgerEntry>.New(),
            UserId = userId,
            Message = item.Message,
            Recipient = item.Recipient
        };

        foreach (var tag in item.Tags)
            entry.Tags.Add(new Tag<LedgerEntry>()
            {
                OwnerId = entry.Id,
                Label = tag
            });

        Db.LedgerEntries.Add(entry);
    }

    public void SaveChanges()
        => Db.SaveChanges();

    public Task SaveChangesAsync()
        => Db.SaveChangesAsync();
}
