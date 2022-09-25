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
        public Dictionary<DateTime, List<TodoItemDto>> ItemsDict { get; set; } = new();

        private readonly IHttpClientFactory _httpClientFactory;
        public event EventHandler? ItemChangedEvent;

        public TodoItemService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Dictionary<DateTime, List<TodoItemDto>>?> InitItems()
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
                ItemsDict.Add(group.Key, group.ToList());
            }

            OnItemChanged(EventArgs.Empty);

            return ItemsDict;
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
            var newItemResponse = await httpClient.GetFromJsonAsync<Response<TodoItemDto>>(location.PathAndQuery);

            if (newItemResponse?.IsSuccess ?? false)
            {
                var item = newItemResponse.Data;
                if (ItemsDict.TryGetValue(item.CreatedAt.Date, out var list))
                {
                    list.Add(item);
                }
                else
                {
                    ItemsDict.Add(item.CreatedAt.Date, new List<TodoItemDto>() { item });
                }
            }

            OnItemChanged(EventArgs.Empty);
        }

        public async Task DeleteItemAsync(TodoItemDto item)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.DeleteAsync($"api/todoitem/delete?itemId={item.Id}");
            response.EnsureSuccessStatusCode();

            var list = ItemsDict.GetValueOrDefault(item.CreatedAt.Date);
            list?.Remove(item);
            if (list?.Count == 0)
            {
                ItemsDict.Remove(item.CreatedAt.Date);
            }

            OnItemChanged(EventArgs.Empty);
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

            var list = ItemsDict.GetValueOrDefault(item.CreatedAt.Date)!;
            foreach (var a in list.Where(t => t.Id == item.Id))
            {
                a.Name = newName;
            }

            OnItemChanged(EventArgs.Empty);
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

            var list = ItemsDict.GetValueOrDefault(item.CreatedAt.Date)!;
            foreach (var a in list.Where(t => t.Id == item.Id))
            {
                a.IsComplete ^= true;
            }

            OnItemChanged(EventArgs.Empty);
        }

        private void OnItemChanged(EventArgs e)
        {
            ItemChangedEvent?.Invoke(this, e);
        }
    }
}
