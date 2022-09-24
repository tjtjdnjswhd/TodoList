using TodoList.Shared.Data.Dtos;

namespace TodoList.Client.Svcs.Interfaces
{
    public interface ITodoItemService
    {
        public List<TodoItemDto> Items { get; }
        public event EventHandler? ItemChangedEvent;

        public Task<List<TodoItemDto>?> InitItems();
        public Task AddItemAsync(string name);
        public Task EditItemNameAsync(int itemId, string newName);
        public Task DeleteItemAsync(int itemId);
        public Task ToggleIsCompleteAsync(int itemId);
    }
}
