using Microsoft.AspNetCore.Authorization;

using System.Net.Http.Json;

using TodoList.Client.Svcs.Interfaces;
using TodoList.Shared.Data.Dtos;
using TodoList.Shared.Models;

namespace TodoList.Client.Svcs.Services
{
    [Authorize]
    public sealed class TodoItemService : ITodoItemService
    {
        public event EventHandler? ItemInitedEvent;
        public event EventHandler? ItemAddedEvent;
        public event EventHandler? ItemDeletedEvent;
        public event EventHandler? ItemUpdatedEvent;

        private readonly Dictionary<DateTime, List<TodoItemDto>> _itemsDict = new();
        private readonly IHttpClientFactory _httpClientFactory;

        public TodoItemService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Dictionary<DateTime, List<TodoItemDto>>?> InitItemsAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync("api/todoitem/get");
            response.EnsureSuccessStatusCode();

            Response<IEnumerable<TodoItemDto>>? items = await response.Content.ReadFromJsonAsync<Response<IEnumerable<TodoItemDto>>>();

            if (items == null || !items.IsSuccess)
            {
                return null;
            }

            var groupByDate = items.Data.GroupBy(t => t.CreatedAt.Date);

            foreach (var group in groupByDate)
            {
                _itemsDict.Add(group.Key, group.ToList());
            }

            ItemInitedEvent?.Invoke(this, EventArgs.Empty);
            return _itemsDict;
        }

        public async Task AddItemAsync(string name)
        {
            var httpClient = _httpClientFactory.CreateClient();
            Dictionary<string, string> values = new()
            {
                { nameof(name), name },
            };
            FormUrlEncodedContent content = new(values);

            HttpResponseMessage message = await httpClient.PostAsync("api/todoitem/post", content);
            message.EnsureSuccessStatusCode();

            Uri location = message.Headers.Location!;
            Response<TodoItemDto>? newItemResponse = await httpClient.GetFromJsonAsync<Response<TodoItemDto>>(location.PathAndQuery);

            if (newItemResponse?.IsSuccess ?? false)
            {
                var item = newItemResponse.Data;
                if (_itemsDict.TryGetValue(item.CreatedAt.Date, out var list))
                {
                    list.Add(item);
                }
                else
                {
                    _itemsDict.Add(item.CreatedAt.Date, new List<TodoItemDto>() { item });
                }
            }

            ItemAddedEvent?.Invoke(this, EventArgs.Empty);
        }

        public async Task DeleteItemAsync(TodoItemDto item)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.DeleteAsync($"api/todoitem/delete?itemId={item.Id}");
            response.EnsureSuccessStatusCode();

            var list = _itemsDict.GetValueOrDefault(item.CreatedAt.Date);
            list?.Remove(item);
            if (list?.Count == 0)
            {
                _itemsDict.Remove(item.CreatedAt.Date);
            }

            ItemDeletedEvent?.Invoke(this, EventArgs.Empty);
        }

        public async Task EditItemNameAsync(TodoItemDto item, string newName)
        {
            var httpClient = _httpClientFactory.CreateClient();
            Dictionary<string, string> values = new()
            {
                { "itemId", item.Id.ToString() },
                { nameof(newName), newName }
            };

            FormUrlEncodedContent content = new(values);
            var response = await httpClient.PatchAsync("api/todoitem/patch", content);
            response.EnsureSuccessStatusCode();

            var list = _itemsDict.GetValueOrDefault(item.CreatedAt.Date)!;
            foreach (var a in list.Where(t => t.Id == item.Id))
            {
                a.Name = newName;
            }

            ItemUpdatedEvent?.Invoke(this, EventArgs.Empty);
        }

        public async Task ToggleIsCompleteAsync(TodoItemDto item)
        {
            var httpClient = _httpClientFactory.CreateClient();
            Dictionary<string, string> values = new()
            {
                { "itemId", item.Id.ToString() },
            };

            FormUrlEncodedContent content = new(values);
            var response = await httpClient.PostAsync("api/todoitem/togglecomplete", content);
            response.EnsureSuccessStatusCode();

            var list = _itemsDict.GetValueOrDefault(item.CreatedAt.Date)!;
            foreach (var a in list.Where(t => t.Id == item.Id))
            {
                a.IsComplete ^= true;
            }

            ItemUpdatedEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
