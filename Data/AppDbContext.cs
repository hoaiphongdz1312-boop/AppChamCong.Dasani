using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using NguyenHoaiPhong_1681.Entities;

namespace NguyenHoaiPhong_1681.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<WorkLocation> WorkLocations => Set<WorkLocation>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<OtpSendLog> OtpSendLogs => Set<OtpSendLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<FaceEmbedding> FaceEmbeddings => Set<FaceEmbedding>();
    public DbSet<FaceChallenge> FaceChallenges => Set<FaceChallenge>();
    public DbSet<LocationShare> LocationShares => Set<LocationShare>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Mọi enum lưu dạng string.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(32);

        // DateTime luôn là UTC: đọc từ DB gắn Kind=Utc để JSON có hậu tố Z.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.GoogleSubjectId).IsUnique().HasFilter("[GoogleSubjectId] IS NOT NULL");
            e.Property(x => x.Username).HasMaxLength(64).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.PasswordHash).HasMaxLength(100);
            e.Property(x => x.FullName).HasMaxLength(128).IsRequired();
            e.Property(x => x.AvatarUrl).HasMaxLength(512);
            e.Property(x => x.GoogleSubjectId).HasMaxLength(128);
            e.Property(x => x.BoundDeviceId).HasMaxLength(128);
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        b.Entity<Department>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Description).HasMaxLength(512);
            e.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        b.Entity<Shift>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(64).IsRequired();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        b.Entity<WorkLocation>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Address).HasMaxLength(256);
            e.Property(x => x.WifiBssid).HasMaxLength(32);
            e.Property(x => x.KioskKey).HasMaxLength(32).IsRequired();
            e.Property(x => x.QrSecret).HasColumnType("varbinary(32)").IsRequired();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        b.Entity<ShiftAssignment>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.WorkDate, x.ShiftId }).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Shift).WithMany().HasForeignKey(x => x.ShiftId);
            e.HasOne(x => x.WorkLocation).WithMany().HasForeignKey(x => x.WorkLocationId);
        });

        b.Entity<AttendanceRecord>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.Timestamp });
            e.Property(x => x.DeviceId).HasMaxLength(128);
            e.Property(x => x.WifiBssid).HasMaxLength(32);
            e.Property(x => x.Note).HasMaxLength(512);
            e.Property(x => x.EditReason).HasMaxLength(512);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne(x => x.ShiftAssignment).WithMany().HasForeignKey(x => x.ShiftAssignmentId);
            e.HasOne(x => x.EditedBy).WithMany().HasForeignKey(x => x.EditedById);
        });

        b.Entity<LeaveRequest>(e =>
        {
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.Property(x => x.ReviewNote).HasMaxLength(500);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerId);
        });

        b.Entity<OtpCode>(e =>
        {
            e.HasIndex(x => x.SessionId);
            e.Property(x => x.CodeHash).HasMaxLength(64);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<OtpSendLog>(e =>
        {
            e.Property(x => x.Destination).HasMaxLength(64);
            e.Property(x => x.Error).HasMaxLength(512);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.HasIndex(x => x.TokenHash);
            e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.DeviceId).HasMaxLength(128);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<FaceEmbedding>(e =>
        {
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);

            var converter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<float[], byte[]>(
                v => FloatsToBytes(v),
                v => BytesToFloats(v));
            var comparer = new ValueComparer<float[]>(
                (a, c) => a != null && c != null && a.SequenceEqual(c),
                v => v.Aggregate(0, (h, f) => HashCode.Combine(h, f)),
                v => v.ToArray());
            e.Property(x => x.Vector)
                .HasConversion(converter, comparer)
                .HasColumnType("varbinary(2048)");
        });

        b.Entity<FaceChallenge>(e =>
        {
            e.HasIndex(x => x.Token);
            e.Property(x => x.Actions).HasMaxLength(128);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<LocationShare>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.Timestamp });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        // SQL Server: tránh multiple cascade paths → mọi FK đều Restrict.
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    private static byte[] FloatsToBytes(float[] v)
    {
        var bytes = new byte[v.Length * sizeof(float)];
        Buffer.BlockCopy(v, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] BytesToFloats(byte[] v)
    {
        var floats = new float[v.Length / sizeof(float)];
        Buffer.BlockCopy(v, 0, floats, 0, v.Length);
        return floats;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default) entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}

public class UtcDateTimeConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)) { }
}

public class NullableUtcDateTimeConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v) { }
}
