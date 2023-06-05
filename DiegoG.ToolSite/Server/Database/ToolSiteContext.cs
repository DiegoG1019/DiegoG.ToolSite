using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DiegoG.ToolSite.Server.Database.Models.Base;
using UserSettings = DiegoG.ToolSite.Server.Database.Models.Base.UserSettings;

namespace DiegoG.ToolSite.Server.Database;

public class ToolSiteContext : DbContext
{
    private static bool isInit = false;
    private static readonly object sync = new();

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<MailConfirmationRequest> MailConfirmationRequests => Set<MailConfirmationRequest>();
    public DbSet<ExecutionLogEntry> ExecutionLog => Set<ExecutionLogEntry>();
    public DbSet<ServerInfo> Servers => Set<ServerInfo>();

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    public ToolSiteContext(DbContextOptions<ToolSiteContext> options) : base(options)
    {
        if (isInit is false)
            lock (sync)
                if (isInit is false)
                {
                    Helper.CreateAppDataDirectory();
                    Database.EnsureCreated();
                    isInit = true;
                }

        ChangeTracker.StateChanged += ChangeTracker_StateChanged;
    }

    private void ChangeTracker_StateChanged(object? sender, Microsoft.EntityFrameworkCore.ChangeTracking.EntityStateChangedEventArgs e)
    {
        if (e.NewState != e.OldState
            && e.NewState is not EntityState.Unchanged or EntityState.Detached or EntityState.Deleted
            && e.Entry.Entity is MutableDbModel mdm)
        {
            if (e.NewState is EntityState.Added)
                mdm.CreationDate = DateTimeOffset.Now;
            mdm.LastModifiedDate = DateTimeOffset.Now;
        }
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (ChangeTracker.HasChanges())
        {
            foreach (var entity in ChangeTracker.Entries())
                if (entity.State is not EntityState.Unchanged && entity.Entity is MutableDbModel mutable)
                    if (entity.State is EntityState.Modified)
                        mutable.LastModifiedDate = DateTimeOffset.Now;
                    else if (entity.State is EntityState.Added)
                        mutable.CreationDate = mutable.LastModifiedDate = DateTimeOffset.Now;
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        ConfigureUser(mb.Entity<User>());
        ConfigureRoles(mb.Entity<Role>());
        ConfigureLog(mb.Entity<ExecutionLogEntry>());
        ConfigureMailConfirmationRequest(mb.Entity<MailConfirmationRequest>());
        ConfigureServers(mb.Entity<ServerInfo>());
        ConfigureLedgerEntries(mb.Entity<LedgerEntry>());
    }

    private static void ConfigureLedgerEntries(EntityTypeBuilder<LedgerEntry> mb)
    {
        mb.Property(x => x.Id).HasConversion(Id<LedgerEntry>.Converter);

        mb.HasIndex(x => x.Recipient).IsUnique(false);
        mb.HasIndex(x => x.Category).IsUnique(false);

        mb.HasOne(x => x.User).WithMany(x => x.LedgerEntries).HasForeignKey(x => x.UserId);

        mb.HasMany(x => x.Tags).WithOne(x => x.Owner).HasForeignKey(x => x.OwnerId);

        mb.HasKey(x => x.Id);
    }

    private static void ConfigureServers(EntityTypeBuilder<ServerInfo> mb)
    {
        mb.Property(x => x.Id).HasConversion(Id<ServerInfo>.Converter);
        mb.Property(x => x.HeartbeatInterval).HasConversion(Helper.TimeSpanToLongConverter);
        mb.HasKey(x => x.Id);
    }

    private static void ConfigureLog(EntityTypeBuilder<ExecutionLogEntry> mb)
    {
        mb.ToTable(nameof(ExecutionLog));
        mb.Property(x => x.UserId).HasConversion(Id<User>.Converter);
        mb.Property(x => x.SessionId).HasConversion(Id<Role>.Converter);
        mb.Property(x => x.Id).ValueGeneratedOnAdd();
    }

    private static void ConfigureMailConfirmationRequest(EntityTypeBuilder<MailConfirmationRequest> mb)
    {
        mb.Property(x => x.Id).HasConversion(Id<MailConfirmationRequest>.Converter);
    }

    private static void ConfigureUser(EntityTypeBuilder<User> mb)
    {
        mb.Property(x => x.Id).HasConversion(Id<User>.Converter);
        mb.HasMany(x => x.Roles).WithMany(x => x.Users);
        mb.HasOne(x => x.MailConfirmationRequest).WithOne(x => x.User).HasForeignKey<MailConfirmationRequest>(x => x.UserId);
        mb.HasOne(x => x.UserSettings).WithOne(x => x.User).HasForeignKey<UserSettings>(x => x.Id);
    }

    private static void ConfigureRoles(EntityTypeBuilder<Role> mb)
    {
        mb.Property(x => x.Id).HasConversion(Id<Role>.Converter);
    }
}
