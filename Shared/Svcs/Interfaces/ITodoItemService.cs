using TodoList.Shared.Data.Models;

namespace TodoList.Shared.Svcs.Interfaces
{
    public interface ITodoItemService
    {
        public Task<TodoItem?> GetByIdOrNullAsync(int itemId);
        public IEnumerable<TodoItem> GetByUserId(Guid userId);
        public Task<int> AddAsync(Guid userId, string name);
        public Task EditNameAsync(int itemId, string name);
        public Task DeleteAsync(int itemId);
        public Task CompleteAsync(int itemId);
        public Task UncompleteAsync(int itemId);
    }
}
