using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TodoList.Shared.Data;
using TodoList.Shared.Data.Models;
using TodoList.Shared.Svcs.Interfaces;

namespace TodoList.Shared.Svcs.Services
{
    public sealed class TodoItemService : ITodoItemService
    {
        private TodoListDbContext _dbContext;
        private ILogger<TodoItemService> _logger;

        public TodoItemService(TodoListDbContext dbContext, ILogger<TodoItemService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<TodoItem?> GetByIdOrNullAsync(int itemId)
        {
            return await _dbContext.TodoItems.AsNoTracking().SingleOrDefaultAsync(t => t.Id == itemId);
        }

        public IAsyncEnumerable<TodoItem> GetByUserId(Guid userId)
        {
            return _dbContext.TodoItems.Where(t => t.UserId == userId).AsAsyncEnumerable();
        }

        public IAsyncEnumerable<TodoItem> GetByUserId(Guid userId, int skip = 0, int take = int.MaxValue)
        {
            return _dbContext.TodoItems.Where(t => t.UserId == userId).Skip(skip).Take(take).AsAsyncEnumerable();
        }

        public IEnumerable<TodoItem> GetChangedByUserId(Guid userId)
        {
            return _dbContext.ChangeTracker.Entries<TodoItem>().Select(t => t.Entity).Where(t => t.UserId == userId);
        }

        public async Task<int> AddAsync(string name, Guid userId)
        {
            if (!_dbContext.Users.Any(u => u.Id == userId))
            {
                return -1;
            }
            else
            {
                await _dbContext.TodoItems.AddAsync(new TodoItem(name, userId));
                _dbContext.SaveChanges();
                int itemId = _dbContext.ChangeTracker.Entries<TodoItem>().First().Entity.Id;
                return itemId;
            }
        }

        public async Task EditNameAsync(int itemId, string name)
        {
            TodoItem? item = _dbContext.TodoItems.SingleOrDefault(t => t.Id == itemId);
            if (item != null)
            {
                item.Name = name;
                await _dbContext.SaveChangesAsync();
            }
        }

        public Task DeleteAsync(int itemId)
        {
            _dbContext.TodoItems.Remove(_dbContext.TodoItems.Find(itemId) ?? new TodoItem());
            return _dbContext.SaveChangesAsync();
        }

        public async Task CompleteAsync(int itemId)
        {
            TodoItem? item = _dbContext.TodoItems.SingleOrDefault(t => t.Id == itemId);
            if (item != null)
            {
                item.IsComplete = true;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task UncompleteAsync(int itemId)
        {
            TodoItem? item = _dbContext.TodoItems.SingleOrDefault(t => t.Id == itemId);
            if (item != null)
            {
                item.IsComplete = false;
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
