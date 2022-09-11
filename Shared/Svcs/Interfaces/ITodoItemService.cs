using TodoList.Shared.Data.Models;

namespace TodoList.Shared.Svcs.Interfaces
{
    public interface ITodoItemService
    {
        public Task<TodoItem?> GetByIdOrNullAsync(int itemId);
        public IAsyncEnumerable<TodoItem> GetByUserId(Guid userId);
        public IAsyncEnumerable<TodoItem> GetByUserId(Guid userId, int skip = 0, int take = int.MaxValue);
        public Task<bool> AddAsync(string name, Guid userId);
        public Task EditNameAsync(int itemId, string name);
        public Task DeleteAsync(int itemId);
        public Task CompleteAsync(int itemId);
        public Task UncompleteAsync(int itemId);
    }
}
