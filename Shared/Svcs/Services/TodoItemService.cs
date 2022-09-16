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
            return await _dbContext.TodoItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == itemId);
        }

        public IEnumerable<TodoItem> GetByUserId(Guid userId)
        {
            return _dbContext.TodoItems.Where(t => t.UserId == userId).AsNoTracking();
        }

        public async Task<int> AddAsync(Guid userId, string name)
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
