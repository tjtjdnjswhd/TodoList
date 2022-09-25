using TodoList.Shared.Data.Dtos;

namespace TodoList.Client.Svcs.Interfaces
{
    public interface ITodoItemService
    {
        public Dictionary<DateTime, List<TodoItemDto>> ItemsDict { get; protected set; }
        public event EventHandler? ItemChangedEvent;

        public Task<Dictionary<DateTime, List<TodoItemDto>>?> InitItems();
        public Task AddItemAsync(string name);
        public Task EditItemNameAsync(TodoItemDto item, string newName);
        public Task DeleteItemAsync(TodoItemDto item);
        public Task ToggleIsCompleteAsync(TodoItemDto item);
    }
}
