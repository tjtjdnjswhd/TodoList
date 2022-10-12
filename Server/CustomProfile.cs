using AutoMapper;

using System.Security.Claims;

using TodoList.Shared.Data.Dtos;
using TodoList.Shared.Data.Models;

namespace TodoList.Server
{
    public sealed class CustomProfile : Profile
    {
        public CustomProfile()
        {
            CreateMap<TodoItem, TodoItemDto>();
            CreateMap<Claim, ClaimDto>();
        }
    }
}
