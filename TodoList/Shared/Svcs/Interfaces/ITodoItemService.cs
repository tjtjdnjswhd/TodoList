using TodoList.Shared.Data.Models;

namespace TodoList.Shared.Svcs.Interfaces
{
    public interface ITodoItemService
    {
        public Task<TodoItem?> GetByIdOrNullAsync(int itemId);
        public IAsyncEnumerable<TodoItem> GetItemsByUserId(Guid userId);
        public Task<bool> AddAsync(string name, Guid userId);
        public Task EditNameAsync(int itemId, string name);
        public Task CompleteAsync(int itemId);
        public Task UncompleteAsync(int itemId);
    }
}
