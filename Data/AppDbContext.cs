// DatingApp.Server/Data/AppDbContext.cs
using DatingApp.Server.Models;

using Microsoft.EntityFrameworkCore;

namespace DatingApp.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- УНИКАЛЬНЫЕ ОГРАНИЧЕНИЯ ---
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Login)
                .IsUnique();

            modelBuilder.Entity<Like>()
                .HasIndex(l => new { l.SourceUserId, l.TargetUserId })
                .IsUnique();

            // --- НАСТРОЙКА LIKE ---
            modelBuilder.Entity<Like>()
                .HasOne(l => l.SourceUser)
                .WithMany(u => u.SentLikes)
                .HasForeignKey(l => l.SourceUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Like>()
                .HasOne(l => l.TargetUser)
                .WithMany(u => u.ReceivedLikes)
                .HasForeignKey(l => l.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- НАСТРОЙКА MESSAGE (ОСНОВНАЯ ПРИЧИНА ОШИБКИ) ---
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- СВЯЗЬ USER - USERPROFILE ---
            modelBuilder.Entity<UserProfile>()
                .HasOne(up => up.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<UserProfile>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- ДОПОЛНИТЕЛЬНО: СВЯЗЬ PHOTO - USERPROFILE (если есть) ---
            modelBuilder.Entity<Photo>()
                .HasOne(p => p.UserProfile)
                .WithMany(up => up.Photos)
                .HasForeignKey(p => p.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}