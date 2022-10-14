using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TodoList.Shared.Data;
using TodoList.Shared.Data.Models;
using TodoList.Shared.Svcs.Interfaces;

namespace TodoList.Shared.Svcs.Services
{
    public sealed class TodoItemService : ITodoItemService
    {
        private readonly TodoListDbContext _dbContext;
        private readonly ILogger<TodoItemService> _logger;

        public TodoItemService(TodoListDbContext dbContext, ILogger<TodoItemService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<TodoItem?> GetByIdOrNullAsync(int itemId)
        {
            TodoItem? item = await _dbContext.TodoItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == itemId);
            _logger.LogDebug("Return item. item id: {itemId}", item?.Id ?? null);
            return item;
        }

        public IEnumerable<TodoItem> GetByUserId(Guid userId)
        {
            IEnumerable<TodoItem> items = _dbContext.TodoItems.Where(t => t.UserId == userId).AsNoTracking();
            _logger.LogDebug("Return items. ids: {@itemIds}", items.Select(i => i.Id));
            return items;
        }

        public async Task<int> AddAsync(Guid userId, string name)
        {
            if (!_dbContext.Users.Any(u => u.Id == userId))
            {
                _logger.LogDebug("User not exist. user id: {userId}", userId);
                return -1;
            }
            else
            {
                TodoItem item = new(name, userId);
                await _dbContext.TodoItems.AddAsync(item);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Item added. item id: {itemId}", item.Id);
                return item.Id;
            }
        }

        public async Task EditNameAsync(int itemId, string name)
        {
            TodoItem? item = _dbContext.TodoItems.SingleOrDefault(t => t.Id == itemId);
            if (item != null)
            {
                _logger.LogInformation("Item name changed. item id: {itemId}, old name: {oldName}, new name: {newName}", itemId, item.Name, name);
                item.Name = name;
                await _dbContext.SaveChangesAsync();
            }
            _logger.LogDebug("Item not found. item id: {itemId}", itemId);
        }

        public Task DeleteAsync(int itemId)
        {
            _dbContext.TodoItems.Remove(_dbContext.TodoItems.Find(itemId) ?? new TodoItem());
            _logger.LogInformation("Item deleted. itemId: {itemId}", itemId);
            return _dbContext.SaveChangesAsync();
        }

        public async Task CompleteAsync(int itemId)
        {
            TodoItem? item = _dbContext.TodoItems.SingleOrDefault(t => t.Id == itemId);
            if (item != null)
            {
                _logger.LogInformation("Item IsComplete changed. itemId: {itemId}", itemId);
                item.IsComplete = true;
                await _dbContext.SaveChangesAsync();
            }
            _logger.LogDebug("Item not found. item id: {itemId}", itemId);
        }

        public async Task UncompleteAsync(int itemId)
        {
            TodoItem? item = _dbContext.TodoItems.SingleOrDefault(t => t.Id == itemId);
            if (item != null)
            {
                _logger.LogInformation("Item IsComplete changed. itemId: {itemId}", itemId);
                item.IsComplete = false;
                await _dbContext.SaveChangesAsync();
            }
            _logger.LogDebug("Item not found. item id: {itemId}", itemId);
        }
    }
}
