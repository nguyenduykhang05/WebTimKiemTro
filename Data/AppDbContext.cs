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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure mapping indexes or constraints if needed
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
        }
    }
}
