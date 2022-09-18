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
        private readonly HttpClient _httpClient;

        public TodoItemService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<TodoItemDto>?> GetItemsByUserIdAsync(Guid userId)
        {
            Response<IEnumerable<TodoItemDto>>? items = await _httpClient.GetFromJsonAsync<Response<IEnumerable<TodoItemDto>>>($"api/todoitem/get");
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

        public Task ToggleIsComplete(int itemId)
        {
            throw new NotImplementedException();
        }
    }
}
