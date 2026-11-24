using Microsoft.EntityFrameworkCore;
using CutiApp.Models;

namespace CutiApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;
    public DbSet<LeaveBalance> LeaveBalances { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Username tidak boleh kembar
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // 1 User -> N LeaveRequest, hapus user tidak menghapus pengajuan
        modelBuilder.Entity<LeaveRequest>()
            .HasOne(r => r.User)
            .WithMany(u => u.LeaveRequests)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1 User -> 1 LeaveBalance
        modelBuilder.Entity<LeaveBalance>()
            .HasOne(b => b.User)
            .WithOne(u => u.LeaveBalances)
            .HasForeignKey<LeaveBalance>(b => b.UserId);

        // Data awal (seed)
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Username = "budi.santoso", Password = "budi123", FullName = "Budi Santoso", Role = "Manager" },
            new User { Id = 2, Username = "siti.aminah", Password = "siti123", FullName = "Siti Aminah", Role = "Employee" },
            new User { Id = 3, Username = "andi.wijaya", Password = "andi123", FullName = "Andi Wijaya", Role = "Employee" }
        );

        modelBuilder.Entity<LeaveBalance>().HasData(
            new LeaveBalance { Id = 1, UserId = 2, RemainingDays = 12 },
            new LeaveBalance { Id = 2, UserId = 3, RemainingDays = 12 }
        );
    }
}