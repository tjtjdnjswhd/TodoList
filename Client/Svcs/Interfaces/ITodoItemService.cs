using TodoList.Shared.Data.Dtos;

namespace TodoList.Client.Svcs.Interfaces
{
    public interface ITodoItemService
    {
        public Task<IEnumerable<TodoItemDto>?> GetItemsByUserIdAsync(Guid userId);
        public Task AddItemAsync(string name);
        public Task EditItemNameAsync(int itemId, string newName);
        public Task DeleteItemAsync(int itemId);
        public Task ToggleIsComplete(int itemId);
    }
}
