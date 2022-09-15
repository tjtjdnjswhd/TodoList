using AutoMapper;

using TodoList.Shared.Data.Dtos;
using TodoList.Shared.Data.Models;

namespace TodoList.Server
{
    public class TodoItemProfile : Profile
    {
        public TodoItemProfile()
        {
            CreateMap<TodoItem, TodoItemDto>();
        }
    }
}
