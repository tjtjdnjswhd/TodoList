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
        public List<TodoItemDto> Items { get; } = new();

        private readonly IHttpClientFactory _httpClientFactory;
        public event EventHandler? ItemChangedEvent;

        public TodoItemService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<TodoItemDto>?> InitItems()
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync("api/todoitem/get");
            response.EnsureSuccessStatusCode();

            Response<IEnumerable<TodoItemDto>>? items = await response.Content.ReadFromJsonAsync<Response<IEnumerable<TodoItemDto>>>();
            if (items == null || !items.IsSuccess)
            {
                return null;
            }

            Items.AddRange(items.Data);
            OnItemChanged(EventArgs.Empty);
            return Items;
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
                Items.Add(newItemResponse.Data);
            }

            OnItemChanged(EventArgs.Empty);
        }

        public async Task DeleteItemAsync(int itemId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.DeleteAsync($"api/todoitem/delete?itemId={itemId}");
            response.EnsureSuccessStatusCode();

            Items.Remove(Items.First(t => t.Id == itemId));
            OnItemChanged(EventArgs.Empty);
        }

        public async Task EditItemNameAsync(int itemId, string newName)
        {
            var httpClient = _httpClientFactory.CreateClient();
            Dictionary<string, string> values = new()
            {
                { nameof(itemId), itemId.ToString() },
                { nameof(newName), newName }
            };

            FormUrlEncodedContent content = new(values);
            var response = await httpClient.PatchAsync($"api/todoitem/patch", content);
            response.EnsureSuccessStatusCode();

            foreach (var item in Items.Where(t => t.Id == itemId))
            {
                item.Name = newName;
            }

            OnItemChanged(EventArgs.Empty);
        }

        public async Task ToggleIsCompleteAsync(int itemId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            Dictionary<string, string> values = new()
            {
                { nameof(itemId), itemId.ToString() },
            };
            FormUrlEncodedContent content = new(values);
            var response = await httpClient.PostAsync("api/todoitem/togglecomplete", content);
            response.EnsureSuccessStatusCode();
            foreach (var item in Items.Where(t => t.Id == itemId))
            {
                item.IsComplete ^= true;
            }

            OnItemChanged(EventArgs.Empty);
        }

        private void OnItemChanged(EventArgs e)
        {
            ItemChangedEvent?.Invoke(this, e);
        }
    }
}
