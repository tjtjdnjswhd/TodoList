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

        public async Task<bool> AddAsync(string name, Guid userId)
        {
            if (!_dbContext.Users.Any(u => u.Id == userId))
            {
                return false;
            }
            else
            {
                await _dbContext.TodoItems.AddAsync(new TodoItem(name, userId));
                await _dbContext.SaveChangesAsync();
                return true;
            }
        }

        public async Task CompleteAsync(int itemId)
        {
            TodoItem? item = _dbContext.TodoItems.SingleOrDefault(t => t.Id == itemId);
            if (item != null)
            {
                item.IsComplete = true;
                _dbContext.TodoItems.Update(item);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task EditNameAsync(int itemId, string name)
        {
            TodoItem? item = _dbContext.TodoItems.SingleOrDefault(t => t.Id == itemId);
            if (item != null)
            {
                item.Name = name;
                _dbContext.TodoItems.Update(item);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<TodoItem?> GetByIdOrNullAsync(int itemId)
        {
            return await _dbContext.TodoItems.SingleOrDefaultAsync(t => t.Id == itemId);
        }

        public IAsyncEnumerable<TodoItem> GetItemsByUserId(Guid userId)
        {
            return _dbContext.TodoItems.Where(t => t.UserId == userId).AsAsyncEnumerable();
        }

        public async Task UncompleteAsync(int itemId)
        {
            TodoItem? item = _dbContext.TodoItems.SingleOrDefault(t => t.Id == itemId);
            if (item != null)
            {
                item.IsComplete = false;
                _dbContext.TodoItems.Update(item);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
