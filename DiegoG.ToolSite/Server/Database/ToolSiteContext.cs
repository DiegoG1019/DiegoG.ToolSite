using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DiegoG.ToolSite.Server.Database.Models.Base;
using UserSettingsDbModel = DiegoG.ToolSite.Server.Database.Models.Base.UserSettingsDbModel;

namespace DiegoG.ToolSite.Server.Database;

public class ToolSiteContext : DbContext
{
    private static bool _init = false;

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<MailConfirmationRequest> MailConfirmationRequests => Set<MailConfirmationRequest>();
    public DbSet<ExecutionLogEntry> ExecutionLog => Set<ExecutionLogEntry>();
    public DbSet<ServerInfo> Servers => Set<ServerInfo>();
    public DbSet<PendingContactMessage> ContactMessages => Set<PendingContactMessage>();

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    public ToolSiteContext(DbContextOptions<ToolSiteContext> options) : base(options)
    {
        if(_init is false)
        {
            lock (typeof(ToolSiteContext))
            {
                if (_init is false)
                {
                    if (Database.IsSqlite())
                    {
                        Database.EnsureCreated();
                    }
                    else if (Database.IsSqlServer())
                    {
            #if DEBUG
                        try
                        {
                            Database.Migrate();
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, "An error ocurred while attempting to migrate the database -- due to being in Debug mode; the database will be deleted and migration reattempted");
                            Database.EnsureDeleted();
                            Database.Migrate();
                        }
            #else
                        Database.Migrate();
            #endif
                    }
                    else
                        throw new InvalidOperationException("The backing database is neither SqlServer or Sqlite, this context is not configured to handle any other databases");

                    _init = true;
                }
            }
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
        ConfigureRoles(mb.Entity<Role>());
        ConfigureLog(mb.Entity<ExecutionLogEntry>());
        ConfigureMailConfirmationRequest(mb.Entity<MailConfirmationRequest>());
        ConfigureServers(mb.Entity<ServerInfo>());
        ConfigureLedgerEntries(mb.Entity<LedgerEntry>());
        ConfigureUser(mb.Entity<User>());
        ConfigurePendingContactMessage(mb.Entity<PendingContactMessage>());
    }

    private static void ConfigurePendingContactMessage(EntityTypeBuilder<PendingContactMessage> mb)
    {
        mb.Property(x => x.Id).HasConversion(Id<PendingContactMessage>.Converter);
    }

    private static void ConfigureUser(EntityTypeBuilder<User> mb)
    {
        mb.Property(x => x.Id).HasConversion(Id<User>.Converter);
        mb.HasMany(x => x.Roles).WithMany(x => x.Users);
        mb.HasOne(x => x.MailConfirmationRequest).WithOne(x => x.User).HasForeignKey<MailConfirmationRequest>(x => x.UserId);
        mb.HasOne(x => x.UserSettings).WithOne(x => x.User).HasForeignKey<UserSettingsDbModel>(x => x.Id);
        mb.HasIndex(x => x.Username).IsUnique();
        mb.HasIndex(x => x.Email).IsUnique();
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

    private static void ConfigureRoles(EntityTypeBuilder<Role> mb)
    {
        mb.Property(x => x.Id).HasConversion(Id<Role>.Converter);
    }
}
