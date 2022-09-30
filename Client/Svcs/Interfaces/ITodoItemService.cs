using TodoList.Shared.Data.Dtos;

namespace TodoList.Client.Svcs.Interfaces
{
    public interface ITodoItemService
    {
        public event EventHandler? ItemChangedEvent;

        public Task<Dictionary<DateTime, List<TodoItemDto>>?> InitItemsAsync();
        public List<TodoItemDto>? GetItemsOrNull(DateTime date);
        public void SortItems(DateTime date, Comparison<TodoItemDto> comparison);
        public Task AddItemAsync(string name);
        public Task EditItemNameAsync(TodoItemDto item, string newName);
        public Task DeleteItemAsync(TodoItemDto item);
        public Task ToggleIsCompleteAsync(TodoItemDto item);
    }
}
