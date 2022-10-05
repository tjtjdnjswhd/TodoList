using TodoList.Shared.Data.Dtos;

namespace TodoList.Client.Svcs.Interfaces
{
    public interface ITodoItemService
    {
        public event EventHandler? ItemInitedEvent;
        public event EventHandler? ItemAddedEvent;
        public event EventHandler? ItemDeletedEvent;
        public event EventHandler? ItemUpdatedEvent;

        public Task<Dictionary<DateTime, List<TodoItemDto>>?> InitItemsAsync();
        public Task AddItemAsync(string name);
        public Task EditItemNameAsync(TodoItemDto item, string newName);
        public Task DeleteItemAsync(TodoItemDto item);
        public Task ToggleIsCompleteAsync(TodoItemDto item);
    }
}
