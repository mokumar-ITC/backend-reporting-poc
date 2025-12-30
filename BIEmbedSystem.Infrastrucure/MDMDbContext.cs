using BIEmbedSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BIEmbedSystem.Infrastrucure
{
    public class MDMDbContext : DbContext
    {
        public MDMDbContext(DbContextOptions<MDMDbContext> options)
         : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        public virtual DbSet<Project> Projects { get; set; }
        public virtual DbSet<PBIGroupWorkspaceReport> GroupWorkspaceReports { get; set; }
        public virtual DbSet<PBIMenubarByGroup> MenubarByGroups { get; set; }
        public virtual DbSet<PBIWorkspaceReport> WorkspaceReports { get; set; }
        public virtual DbSet<PBINavigationManagement> NavigationManagements { get; set; }
        public virtual DbSet<PBINavigationUserAccess> NavigationUserAccesses { get; set; }
        public virtual DbSet<CapacitySchedulerModel> Capacity_Scheduler { get; set; }
        public virtual DbSet<ReportSubscription> ReportSubscription { get; set; }

        public virtual DbSet<SemanticScheduler> SemanticSchedulers { get; set; }

        public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public virtual DbSet<SubscriptionFeature> SubscriptionFeatures { get; set; }
        public virtual DbSet<PlanFeature> PlanFeatures { get; set; }
        public virtual DbSet<UserSubscription> UserSubscriptions { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<OrganizationSubscription> OrganizationSubscriptions { get; set; }
        public virtual DbSet<Organization> Organizations { get; set; }
        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }
        public DbSet<UserTracking> UserTrackings { get; set; }
        public virtual DbSet<PowerBiBookmark> PowerBI_Bookmarks { get; set; }
        
        public virtual DbSet<PBIRLSSecurity> PowerBI_Security { get; set; }

        public virtual DbSet<Role> Roles { get; set; }
    }
}
