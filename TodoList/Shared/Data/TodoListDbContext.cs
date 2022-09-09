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
            modelBuilder.Entity<TodoItem>();
            modelBuilder.Entity<Role>();
            modelBuilder.Entity<User>();
        }
    }
}
