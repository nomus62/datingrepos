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

    // Уникальные ограничения
    modelBuilder.Entity<User>()
        .HasIndex(u => u.Login)
        .IsUnique();

    modelBuilder.Entity<Like>()
        .HasIndex(l => new { l.SourceUserId, l.TargetUserId })
        .IsUnique();

    // Настройка связей для Like
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

    // Настройка связей для Message (это и есть основная проблема)
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

    // Связь User - UserProfile (она уже настроена через атрибут, но можно продублировать)
    modelBuilder.Entity<UserProfile>()
        .HasOne(up => up.User)
        .WithOne(u => u.Profile)
        .HasForeignKey<UserProfile>(up => up.UserId)
        .OnDelete(DeleteBehavior.Cascade);
}
    }
}