using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InvigilatorSchedulerStandard.Services;

namespace InvigilatorSchedulerStandard.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService) 
        : base(options) 
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<ExamSession> ExamSessions => Set<ExamSession>();
    public DbSet<TeacherRestrictedExamSession> TeacherRestrictedExamSessions => Set<TeacherRestrictedExamSession>();
    public DbSet<RuleConfig> RuleConfigs => Set<RuleConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global Query Filter
        modelBuilder.Entity<Grade>().HasQueryFilter(e => e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<Teacher>().HasQueryFilter(e => e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<ExamSession>().HasQueryFilter(e => e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<RuleConfig>().HasQueryFilter(e => e.UserId == _currentUserService.UserId);

        modelBuilder.Entity<TeacherRestrictedExamSession>()
            .HasKey(x => new { x.TeacherId, x.ExamSessionId });

        modelBuilder.Entity<TeacherRestrictedExamSession>()
            .HasOne(x => x.Teacher)
            .WithMany(t => t.RestrictedExamSessions)
            .HasForeignKey(x => x.TeacherId);

        modelBuilder.Entity<TeacherRestrictedExamSession>()
            .HasOne(x => x.ExamSession)
            .WithMany()
            .HasForeignKey(x => x.ExamSessionId);

        // Ensure Unicode columns (SQL Server uses NVARCHAR for string by default in EF Core)
        // Seed default rules
        // Note: Seeding with fixed ID might conflict if multiple users try to own it. 
        // For now, we disable seeding or we make it owned by a system admin? 
        // Since we want every user to have their own rules, we shouldn't seed generic rules with fixed IDs here 
        // unless they are templates. But current logic relies on specific IDs?
        // Let's comment out seeding for now OR modify logic to seed on user registration.
        // For this task, I will comment out seeding to avoid 'UserId' being null violation if required 
        // OR just leave them. If I leave them, UserId is null. Query filter 'UserId == current' will hide them from users.
        // So users won't see default rules. This might break the app if it expects rules.
        // Solution: Seed logic should be moved to "User Registration" or "First Access". 
        // I will commented out seeding here.
        
        /* 
        modelBuilder.Entity<RuleConfig>().HasData(
            new RuleConfig
            {
                Id = 1,
                Code = RuleCodes.Settings,
                JsonValue = "{\"defaultInvigilatorsPerExam\":2,\"backupInvigilatorsPerDay\":1,\"randomSeed\":5,\"fairnessEnabled\":true}"
            },
           ...
        );
        */
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IUserOwnedEntity>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.UserId))
            {
                entry.Entity.UserId = _currentUserService.UserId;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
