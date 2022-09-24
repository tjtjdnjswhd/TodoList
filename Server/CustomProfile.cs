using AutoMapper;

using System.Security.Claims;

using TodoList.Shared.Data.Dtos;
using TodoList.Shared.Data.Models;
using TodoList.Shared.Models;

namespace TodoList.Server
{
    public sealed class CustomProfile : Profile
    {
        public CustomProfile()
        {
            CreateMap<TodoItem, TodoItemDto>();
            CreateMap<User, UserInfo>();
            CreateMap<Claim, ClaimDto>();
        }
    }
}
