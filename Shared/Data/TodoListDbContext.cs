#nullable disable
using Microsoft.EntityFrameworkCore;

using TodoList.Shared.Data.Models;

namespace TodoList.Shared.Data
{
    public sealed class TodoListDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<TodoItem> TodoItems { get; set; }

        public TodoListDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TodoItem>(builder =>
            {
                builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETDATE()");
                builder.Property(t => t.IsComplete).HasDefaultValue(false);
            });
            modelBuilder.Entity<Role>().HasData(
                new Role()
                {
                    Name = "User",
                    Priority = 1
                },
                new Role()
                {
                    Name = "Admin",
                    Priority = 0
                });
            modelBuilder.Entity<User>(builder =>
            {
                builder.Property(u => u.SignupDate).HasDefaultValueSql("GETDATE()");
            });
        }
    }
}
