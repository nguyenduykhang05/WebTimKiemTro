using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;

namespace SmartRoomFinder.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UserModel> Users { get; set; } = null!;
        public DbSet<RoomModel> Rooms { get; set; } = null!;
        public DbSet<ChatModel> Chats { get; set; } = null!;
        public DbSet<MessageModel> Messages { get; set; } = null!;
        public DbSet<ApplicationModel> Applications { get; set; } = null!;
        public DbSet<ReviewModel> Reviews { get; set; } = null!;
        public DbSet<ReportModel> Reports { get; set; } = null!;
        public DbSet<LandmarkModel> Landmarks { get; set; } = null!;
        public DbSet<NotificationModel> Notifications { get; set; } = null!;
        public DbSet<RoomImageModel> RoomImages { get; set; } = null!;
        public DbSet<AIChatLogModel> AIChatLogs { get; set; } = null!;
        public DbSet<UserFavoriteRoomModel> UserFavorites { get; set; } = null!;
        public DbSet<SupportTicketModel> SupportTickets { get; set; } = null!;
        public DbSet<SystemSettingModel> SystemSettings { get; set; } = null!;
        public DbSet<DepositModel> Deposits { get; set; } = null!;

        // -------------------------------------------------------
        // New: KYC & Appointments
        // -------------------------------------------------------
        public DbSet<KycProfileModel> KycProfiles { get; set; } = null!;
        public DbSet<AppointmentModel> Appointments { get; set; } = null!;
        public DbSet<ServiceTransactionModel> ServiceTransactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserModel>().HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<RoomImageModel>()
                .HasOne(ri => ri.Room)
                .WithMany(r => r.Images)
                .HasForeignKey(ri => ri.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserFavoriteRoomModel>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserFavoriteRoomModel>()
                .HasOne(f => f.Room)
                .WithMany()
                .HasForeignKey(f => f.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // KYC: quan hệ 1-1 với User. Mỗi Landlord chỉ có 1 hồ sơ KYC.
            modelBuilder.Entity<KycProfileModel>()
                .HasOne(k => k.User)
                .WithOne()
                .HasForeignKey<KycProfileModel>(k => k.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Chỉ cho phép 1 hồ sơ KYC trên mỗi UserId
            modelBuilder.Entity<KycProfileModel>()
                .HasIndex(k => k.UserId)
                .IsUnique();

            // Appointments: quan hệ n-1 với User (Tenant) và Room
            modelBuilder.Entity<AppointmentModel>()
                .HasOne(a => a.Tenant)
                .WithMany()
                .HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AppointmentModel>()
                .HasOne(a => a.Room)
                .WithMany()
                .HasForeignKey(a => a.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deposits: configure navigation explicitly to avoid multiple cascade paths
            modelBuilder.Entity<DepositModel>()
                .HasOne(d => d.Room)
                .WithMany()
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DepositModel>()
                .HasOne(d => d.Renter)
                .WithMany()
                .HasForeignKey(d => d.RenterId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
