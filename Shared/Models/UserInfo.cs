﻿#nullable disable

namespace TodoList.Shared.Models
{
    public class UserInfo
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string RoleName { get; set; }
    }
}
