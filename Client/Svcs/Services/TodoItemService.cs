using Microsoft.AspNetCore.Authorization;

using System.Net.Http.Json;

using TodoList.Client.Svcs.Interfaces;
using TodoList.Shared.Data.Dtos;
using TodoList.Shared.Models;

namespace TodoList.Client.Svcs.Services
{
    [Authorize]
    public class TodoItemService : ITodoItemService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TodoItemService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<TodoItemDto>?> GetItemsByUserIdAsync(Guid userId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync("api/todoitem/get");

            Response<IEnumerable<TodoItemDto>>? items = await response.Content.ReadFromJsonAsync<Response<IEnumerable<TodoItemDto>>>();
            if (items == null || !items.IsSuccess)
            {
                return null;
            }

            return items.Data;
        }

        public Task AddItemAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task DeleteItemAsync(int itemId)
        {
            throw new NotImplementedException();
        }

        public Task EditItemNameAsync(int itemId, string newName)
        {
            throw new NotImplementedException();
        }

        public Task ToggleIsCompleteAsync(int itemId)
        {
            throw new NotImplementedException();
        }
    }
}
