using AutoMapper;

using TodoList.Shared.Data.Dtos;
using TodoList.Shared.Data.Models;
using TodoList.Shared.Models;

namespace TodoList.Server
{
    public class CustomProfile : Profile
    {
        public CustomProfile()
        {
            CreateMap<TodoItem, TodoItemDto>();
            CreateMap<User, UserInfo>();
        }
    }
}
